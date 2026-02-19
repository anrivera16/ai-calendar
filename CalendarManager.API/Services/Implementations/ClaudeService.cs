using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using CalendarManager.API.Data;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;
using System.Text.Json.Nodes;
using CommonTool = Anthropic.SDK.Common.Tool;

namespace CalendarManager.API.Services.Implementations;

public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _anthropicClient;
    private readonly IGoogleCalendarService _calendarService;
    private readonly ILogger<ClaudeService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private string? _currentUserId;

    public ClaudeService(
        IConfiguration configuration,
        IGoogleCalendarService calendarService,
        ILogger<ClaudeService> logger,
        AppDbContext context)
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") 
                    ?? configuration["Claude:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Claude API key not found. Set CLAUDE_API_KEY environment variable or Claude:ApiKey in configuration.");

        _anthropicClient = new AnthropicClient(apiKey);
        _calendarService = calendarService;
        _logger = logger;
        _configuration = configuration;
        _context = context;
    }

    public async Task<ChatResponse> ProcessMessageAsync(string message, string userId, string? conversationId = null)
    {
        try
        {
            _logger.LogInformation("Processing message for user {UserId}: {Message}", userId, message);

            // Get user from database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
            if (user == null)
            {
                return new ChatResponse
                {
                    Message = "User not found. Please authenticate first.",
                    Success = false,
                    ErrorMessage = "Authentication required",
                    Type = MessageType.Error
                };
            }

            // Use Claude with function calling
            var response = await ProcessWithClaudeAsync(message, user.Id, userId);
            
            return new ChatResponse
            {
                Message = response,
                Success = true,
                Type = MessageType.Success,
                ConversationId = conversationId ?? Guid.NewGuid().ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for user {UserId}", userId);
            return new ChatResponse
            {
                Message = "Sorry, I encountered an error processing your request. Please try again.",
                Success = false,
                ErrorMessage = ex.Message,
                Type = MessageType.Error
            };
        }
    }

    private async Task<string> ProcessWithClaudeAsync(string message, Guid userId, string userEmail)
    {
        try
        {
            // Store current user context for tool execution
            _currentUserId = userEmail;
            
            _logger.LogInformation("Starting Claude processing for user: {UserEmail}", userEmail);
            
            // Use Claude API
            var response = await CallClaudeAPIAsync(message);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessWithClaudeAsync: {Error}", ex.Message);
            return ProcessLocalAI(message); // Fall back to local processing
        }
    }

    private async Task<string> CallClaudeAPIAsync(string userMessage)
    {
        try
        {
            _logger.LogInformation("Making Claude API call for message: {Message}", userMessage);
            
            var messages = new List<Message>()
            {
                new Message(RoleType.User, userMessage)
            };

            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 1000,
                Model = AnthropicModels.Claude35Haiku,
                Stream = false,
                Temperature = 0.7m,
                System = new List<SystemMessage>() { new SystemMessage(GetSystemPrompt()) }
                // Tools will be added when we get the SDK working properly
            };

            _logger.LogInformation("Calling Claude API with model: {Model}", parameters.Model);
            var response = await _anthropicClient.Messages.GetClaudeMessageAsync(parameters);
            _logger.LogInformation("Claude API call successful. Response length: {Length}", response.Content.Count);

            // For now, process as simple text response
            var textContent = response.Content.OfType<TextContent>().FirstOrDefault();
            var result = textContent?.Text ?? "I received your request but couldn't process it properly.";
            
            _logger.LogInformation("Claude response: {Response}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API call failed: {Error}", ex.Message);
            // Return a clear message that we're using Claude AI (even if it fell back)
            return $"I'm Claude, your AI calendar assistant! I processed your message: '{userMessage}'\n\n" +
                   "I understand you're looking for calendar help. While I'm getting fully set up with function calling, " +
                   "I can provide intelligent responses about calendar management.\n\n" +
                   $"Error details: {ex.Message}";
        }
    }

    private string ProcessLocalAI(string message)
    {
        // Enhanced local processing as a fallback while we fix the Claude integration
        var lowerMessage = message.ToLower();

        if (lowerMessage.Contains("schedule") || lowerMessage.Contains("create") || lowerMessage.Contains("book"))
        {
            if (lowerMessage.Contains("meeting") || lowerMessage.Contains("appointment") || 
                lowerMessage.Contains("lunch") || lowerMessage.Contains("dinner"))
            {
                return "I'd love to help you schedule that! To create a calendar event, I need:\n\n" +
                       "• **What**: Meeting title or description\n" +
                       "• **When**: Date and time (e.g., 'tomorrow at 2pm')\n" +
                       "• **How long**: Duration (defaults to 1 hour)\n\n" +
                       "For example: 'Schedule lunch with Sarah tomorrow at 1pm'\n\n" +
                       "*Note: Enhanced AI scheduling with Claude is coming soon!*";
            }
        }

        if (lowerMessage.Contains("show") || lowerMessage.Contains("view") || 
            lowerMessage.Contains("list") || lowerMessage.Contains("events"))
        {
            return "I can help you view your calendar events! Currently, you can:\n\n" +
                   "• Use the 'View Calendar Events' button in the dashboard\n" +
                   "• Check your Google Calendar directly\n\n" +
                   "*Smart calendar querying with Claude is being implemented!*";
        }

        if (lowerMessage.Contains("help") || lowerMessage.Contains("what can you do"))
        {
            return "🤖 **AI Calendar Assistant** (Enhanced with Claude - Coming Soon!)\n\n" +
                   "I can help you with:\n" +
                   "✅ **Schedule meetings** - Natural language event creation\n" +
                   "✅ **View events** - Smart calendar browsing\n" +
                   "✅ **Find free time** - Intelligent scheduling suggestions\n" +
                   "🔄 **Cancel/modify events** - Easy event management\n\n" +
                   "*Currently using enhanced pattern matching. Full Claude integration in progress!*";
        }

        if (lowerMessage.Contains("cancel") || lowerMessage.Contains("delete") || lowerMessage.Contains("remove"))
        {
            return "🗑️ **Event Management**\n\n" +
                   "Event deletion and modification features are being developed with Claude AI integration.\n\n" +
                   "For now, you can:\n" +
                   "• Use Google Calendar directly to modify events\n" +
                   "• Use the dashboard to create new events\n\n" +
                   "*Smart event management coming soon!*";
        }

        // Default response
        return "👋 I'm your AI Calendar Assistant!\n\n" +
               "I'm currently being enhanced with Claude AI for better natural language understanding.\n\n" +
               "Try asking me to:\n" +
               "• 'Schedule a meeting with John tomorrow'\n" +
               "• 'Show my events for this week'\n" +
               "• 'Help me find time for a 2-hour project meeting'\n\n" +
               "*Full Claude integration coming very soon!*";
    }

    // Tool processing methods temporarily commented out
    // Will be restored when SDK tool calling is properly configured

    private async Task ExecuteCalendarActionsFromResponse(string response, Guid userId)
    {
        // This method is no longer needed as we handle tool execution in ProcessClaudeResponse
        await Task.CompletedTask;
    }

    // Temporarily commented out until SDK tool calling is properly configured
    // private List<CommonTool> GetCalendarTools() { ... }

    private string GetSystemPrompt()
    {
        return @"You are Claude, an AI assistant that helps users manage their Google Calendar through natural language interactions.

You are friendly, helpful, and conversational. You can help users with:
- Creating calendar events (meetings, appointments, reminders)
- Viewing existing events and schedules
- Finding free time slots for meetings
- Managing their overall schedule

Current date and time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"

When users ask about calendar operations, provide helpful responses and guidance. Parse dates and times intelligently (e.g., 'tomorrow at 2pm', 'next Friday', 'in 2 hours'). 

Always respond in a natural, conversational way and offer to help with specific calendar tasks. If you need more information to complete a task, ask clarifying questions.";
    }

    // Implementation of interface methods for future use
    public async Task<CalendarAction> CreateCalendarEventAsync(CreateEventToolCall toolCall, string userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
            if (user == null)
            {
                return new CalendarAction
                {
                    Type = "create_calendar_event",
                    Executed = false,
                    ErrorMessage = "User not found"
                };
            }

            var createEventDto = new CreateEventDto
            {
                Title = toolCall.Title,
                Start = toolCall.StartTime,
                End = toolCall.EndTime,
                Description = toolCall.Description,
                Location = toolCall.Location
            };

            var createdEvent = await _calendarService.CreateEventAsync(user.Id, createEventDto);

            return new CalendarAction
            {
                Type = "create_calendar_event",
                Data = createdEvent,
                Executed = true,
                Result = $"Created event '{toolCall.Title}' on {toolCall.StartTime:MM/dd/yyyy} at {toolCall.StartTime:HH:mm}"
            };
        }
        catch (Exception ex)
        {
            return new CalendarAction
            {
                Type = "create_calendar_event",
                Executed = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<CalendarAction> ListCalendarEventsAsync(ListEventsToolCall toolCall, string userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
            if (user == null)
            {
                return new CalendarAction
                {
                    Type = "list_calendar_events",
                    Executed = false,
                    ErrorMessage = "User not found"
                };
            }

            var startTime = toolCall.StartDate ?? DateTime.Today;
            var endTime = toolCall.EndDate ?? DateTime.Today.AddDays(7);

            var events = await _calendarService.GetEventsAsync(user.Id, startTime, endTime);

            var eventSummary = events.Take(toolCall.MaxResults ?? 10)
                .Select(e => $"• {e.Title} - {e.Start:MM/dd/yyyy HH:mm}")
                .ToList();

            return new CalendarAction
            {
                Type = "list_calendar_events",
                Data = events,
                Executed = true,
                Result = eventSummary.Any() ? 
                    $"Found {events.Count} events:\n{string.Join("\n", eventSummary)}" : 
                    "No events found for the specified time period."
            };
        }
        catch (Exception ex)
        {
            return new CalendarAction
            {
                Type = "list_calendar_events",
                Executed = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<CalendarAction> FindFreeTimeAsync(FindFreeTimeToolCall toolCall, string userId)
    {
        // Simplified implementation for now
        await Task.CompletedTask;
        
        return new CalendarAction
        {
            Type = "find_free_time",
            Executed = true,
            Result = "Free time finding is being enhanced with Claude AI. Check back soon!"
        };
    }
}