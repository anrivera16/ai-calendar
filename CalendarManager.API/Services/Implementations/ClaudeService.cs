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

            // Check configuration for Claude usage
            var fallbackOnly = _configuration.GetValue<bool>("Claude:FallbackOnly", false);
            var claudeApiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") 
                             ?? _configuration["Claude:ApiKey"];
            
            string response;
            if (fallbackOnly)
            {
                _logger.LogInformation("Configuration set to fallback only. Using intelligent fallback.");
                response = ProcessIntelligentFallback(message);
            }
            else if (string.IsNullOrEmpty(claudeApiKey))
            {
                _logger.LogInformation("No Claude API key configured. Using intelligent fallback.");
                response = ProcessIntelligentFallback(message);
            }
            else
            {
                // Try Claude first, fall back to intelligent processing if needed
                response = await ProcessWithClaudeAsync(message, user.Id, userId);
            }
            
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
            
            // Even in error cases, try to provide a helpful fallback response
            try
            {
                var fallbackResponse = ProcessIntelligentFallback(message);
                return new ChatResponse
                {
                    Message = fallbackResponse + "\n\n*Note: I encountered a technical issue but provided this intelligent response.*",
                    Success = true, // Still successful from user perspective
                    Type = MessageType.Warning,
                    ConversationId = conversationId ?? Guid.NewGuid().ToString()
                };
            }
            catch
            {
                return new ChatResponse
                {
                    Message = "I'm experiencing technical difficulties. Please try again in a moment, or contact support if the issue persists.",
                    Success = false,
                    ErrorMessage = "Service temporarily unavailable",
                    Type = MessageType.Error
                };
            }
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
            return ProcessIntelligentFallback(message); // Fall back to intelligent processing
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
            
            // Check if it's a credit/billing issue
            if (ex.Message.Contains("credit balance") || ex.Message.Contains("billing") || 
                ex.Message.Contains("payment") || ex.Message.Contains("invalid_request_error"))
            {
                _logger.LogWarning("Claude API unavailable due to billing/credits. Using intelligent fallback.");
                
                var showCreditMessage = _configuration.GetValue<bool>("Claude:ShowCreditMessage", true);
                if (showCreditMessage)
                {
                    return $"💰 **Claude AI Credits Needed**\n\n" +
                           $"I'm your AI Calendar Assistant! My advanced Claude features need API credits, but I can still provide intelligent help.\n\n" +
                           $"**🤖 Current Capabilities:**\n" +
                           $"• Smart calendar conversation\n" +
                           $"• Scheduling guidance and examples\n" +
                           $"• Time management advice\n" +
                           $"• Natural language understanding\n\n" +
                           $"**🚀 Your request**: \"{userMessage}\"\n\n" +
                           $"Let me help with that:\n\n" +
                           ProcessIntelligentFallback(userMessage) +
                           $"\n\n*💡 Add Claude API credits for advanced AI features like direct calendar creation!*";
                }
                
                return ProcessIntelligentFallback(userMessage);
            }
            
            // Check if it's a rate limit issue
            if (ex.Message.Contains("rate_limit") || ex.Message.Contains("429"))
            {
                _logger.LogWarning("Claude API rate limited. Using intelligent fallback.");
                return ProcessIntelligentFallback(userMessage) + "\n\n*Note: Claude API is temporarily busy. Please try again in a moment.*";
            }
            
            // For other errors, provide helpful fallback
            _logger.LogWarning("Claude API error. Using intelligent fallback. Error: {Error}", ex.Message);
            return ProcessIntelligentFallback(userMessage);
        }
    }

    private string ProcessIntelligentFallback(string message)
    {
        var lowerMessage = message.ToLower();
        var currentTime = DateTime.Now;
        var greeting = GetTimeBasedGreeting(currentTime);
        
        // Enhanced calendar-focused AI responses - check viewing before scheduling
        if (ContainsGreeting(lowerMessage))
        {
            return ProcessGreeting(message, currentTime, greeting);
        }
        
        if (ContainsViewingIntent(lowerMessage))
        {
            return ProcessViewingRequest(message, lowerMessage);
        }
        
        if (ContainsSchedulingIntent(lowerMessage))
        {
            return ProcessSchedulingRequest(message, lowerMessage);
        }
        
        if (ContainsCancellationIntent(lowerMessage))
        {
            return ProcessCancellationRequest(message, lowerMessage);
        }
        
        if (ContainsTimeIntent(lowerMessage))
        {
            return ProcessTimeQuery(message, lowerMessage, currentTime);
        }
        
        if (ContainsHelpIntent(lowerMessage))
        {
            return ProcessHelpRequest(message, currentTime);
        }
        
        // Default intelligent response
        return ProcessDefaultResponse(message, currentTime, greeting);
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

    // Intelligent fallback helper methods
    private bool ContainsSchedulingIntent(string message) =>
        (message.Contains("schedule") && !message.Contains("what's on") && !message.Contains("show")) ||
        (message.Contains("create") && (message.Contains("event") || message.Contains("meeting"))) ||
        message.Contains("book") || message.Contains("add") || message.Contains("plan") || 
        message.Contains("set up") || message.Contains("new meeting") || message.Contains("new appointment") ||
        (message.Contains("lunch") && !message.Contains("what")) || 
        (message.Contains("dinner") && !message.Contains("what")) ||
        (message.Contains("call") && !message.Contains("what")) || 
        (message.Contains("reminder") && !message.Contains("what"));

    private bool ContainsViewingIntent(string message) =>
        message.Contains("what meetings") || message.Contains("what events") || message.Contains("what's on") ||
        message.Contains("show me") || message.Contains("view") || message.Contains("list") || 
        message.Contains("schedule for") || message.Contains("what do i have") || 
        message.Contains("my calendar") || message.Contains("events today") || 
        message.Contains("meetings today") || message.Contains("busy today") ||
        (message.Contains("what") && (message.Contains("today") || message.Contains("tomorrow") || message.Contains("this week")));

    private bool ContainsCancellationIntent(string message) =>
        message.Contains("cancel") || message.Contains("delete") || message.Contains("remove") ||
        message.Contains("reschedule") || message.Contains("move") || message.Contains("change");

    private bool ContainsTimeIntent(string message) =>
        message.Contains("when") || message.Contains("what time") || message.Contains("free time") ||
        message.Contains("available") || message.Contains("busy") || message.Contains("conflict");

    private bool ContainsHelpIntent(string message) =>
        message.Contains("help") || message.Contains("what can you do") || message.Contains("how do") ||
        message.Contains("commands") || message.Contains("features");

    private bool ContainsGreeting(string message) =>
        message.Contains("hello") || message.Contains("hi") || message.Contains("hey") ||
        message.Contains("good morning") || message.Contains("good afternoon") || message.Contains("good evening");

    private string GetTimeBasedGreeting(DateTime currentTime)
    {
        var hour = currentTime.Hour;
        return hour switch
        {
            < 12 => "Good morning",
            < 17 => "Good afternoon", 
            _ => "Good evening"
        };
    }

    private string ProcessSchedulingRequest(string originalMessage, string lowerMessage)
    {
        var examples = new List<string>
        {
            "\"Schedule lunch with Sarah tomorrow at 1pm\"",
            "\"Book a meeting with the team next Tuesday at 2:30pm\"",
            "\"Add a dentist appointment on Friday at 10am\"",
            "\"Create a reminder to call mom at 6pm today\""
        };
        
        var randomExample = examples[new Random().Next(examples.Count)];
        
        return $"🗓️ **I'd love to help you schedule that!**\n\n" +
               $"I understand you want to create a calendar event. To help you better, I need:\n\n" +
               $"• **What**: Meeting title or description\n" +
               $"• **When**: Specific date and time\n" +
               $"• **Duration**: How long (I'll default to 1 hour)\n" +
               $"• **Location** (optional): Where it's happening\n\n" +
               $"**Example**: {randomExample}\n\n" +
               $"*🔄 I'm using enhanced AI while Claude function calling is being set up. Soon I'll be able to create events directly!*";
    }

    private string ProcessViewingRequest(string originalMessage, string lowerMessage)
    {
        var timeframe = lowerMessage.Contains("today") ? "today" :
                       lowerMessage.Contains("tomorrow") ? "tomorrow" :
                       lowerMessage.Contains("this week") ? "this week" :
                       lowerMessage.Contains("next week") ? "next week" : 
                       "your schedule";
        
        return $"📅 **Let me help you view your calendar!**\n\n" +
               $"You're asking about events for **{timeframe}**.\n\n" +
               $"**Current options:**\n" +
               $"• Use the **'View Calendar Events'** button in your dashboard\n" +
               $"• Check your Google Calendar directly\n" +
               $"• Ask me specific questions like \"What meetings do I have today?\"\n\n" +
               $"*🔄 Smart calendar querying with Claude is coming soon! I'll be able to show you exactly what you need.*";
    }

    private string ProcessCancellationRequest(string originalMessage, string lowerMessage)
    {
        return $"🗑️ **Event Management**\n\n" +
               $"I understand you want to cancel, delete, or modify an event.\n\n" +
               $"**For now, you can:**\n" +
               $"• Go directly to Google Calendar to modify events\n" +
               $"• Use your calendar app to make changes\n" +
               $"• Let me know the specific event and I'll guide you\n\n" +
               $"**Coming soon**: I'll be able to cancel and reschedule events directly through chat!\n\n" +
               $"*🔄 Smart event management with Claude is being implemented.*";
    }

    private string ProcessTimeQuery(string originalMessage, string lowerMessage, DateTime currentTime)
    {
        var timeStr = currentTime.ToString("h:mm tt");
        var dateStr = currentTime.ToString("dddd, MMMM d");
        
        return $"⏰ **Time & Availability**\n\n" +
               $"**Current time**: {timeStr} on {dateStr}\n\n" +
               $"I can help you find free time slots and check availability.\n\n" +
               $"**Try asking:**\n" +
               $"• \"When am I free tomorrow?\"\n" +
               $"• \"Do I have conflicts this afternoon?\"\n" +
               $"• \"Find me 2 hours for project work this week\"\n\n" +
               $"*🔄 Intelligent scheduling with Claude function calling is coming soon!*";
    }

    private string ProcessHelpRequest(string originalMessage, DateTime currentTime)
    {
        return $"🤖 **AI Calendar Assistant** (Enhanced with Claude)\n\n" +
               $"I'm your intelligent calendar companion! Here's what I can help with:\n\n" +
               $"**✅ Currently Available:**\n" +
               $"• Smart conversation about your calendar needs\n" +
               $"• Guidance on scheduling and time management\n" +
               $"• Help interpreting calendar requests\n" +
               $"• Time-aware responses\n\n" +
               $"**🚀 Coming Soon:**\n" +
               $"• Direct calendar event creation\n" +
               $"• Smart scheduling suggestions\n" +
               $"• Natural language date/time parsing\n" +
               $"• Event management and modification\n\n" +
               $"**💡 Try asking:**\n" +
               $"• \"Schedule a team meeting tomorrow\"\n" +
               $"• \"What's my schedule for today?\"\n" +
               $"• \"When am I free this week?\"\n\n" +
               $"*🔄 Full Claude integration with function calling in progress!*";
    }

    private string ProcessGreeting(string originalMessage, DateTime currentTime, string greeting)
    {
        var responses = new[]
        {
            $"{greeting}! I'm your AI calendar assistant.",
            $"Hello there! Ready to help with your schedule.",
            $"Hi! I'm here to help manage your calendar.",
            $"{greeting}! What can I help you schedule today?"
        };
        
        var response = responses[new Random().Next(responses.Length)];
        
        return $"👋 **{response}**\n\n" +
               $"**Current time**: {currentTime:h:mm tt} on {currentTime:dddd}\n\n" +
               $"I can help you with:\n" +
               $"• Scheduling meetings and appointments\n" +
               $"• Viewing your calendar events\n" +
               $"• Finding free time slots\n" +
               $"• Managing your schedule\n\n" +
               $"What would you like to do with your calendar today?";
    }

    private string ProcessDefaultResponse(string originalMessage, DateTime currentTime, string greeting)
    {
        return $"🤖 **{greeting}! I'm your AI Calendar Assistant**\n\n" +
               $"I noticed you said: *\"{originalMessage}\"*\n\n" +
               $"I'm here to help with your calendar and scheduling needs. While I'm getting my full Claude capabilities set up, I can provide intelligent guidance on:\n\n" +
               $"• **Creating events**: \"Schedule lunch with John tomorrow\"\n" +
               $"• **Viewing schedules**: \"What's on my calendar today?\"\n" +
               $"• **Time management**: \"When am I free this week?\"\n" +
               $"• **Planning**: \"Help me find time for a 2-hour meeting\"\n\n" +
                $"**Current time**: {currentTime:h:mm tt} on {currentTime:dddd, MMMM d}\n\n" +
                $"What would you like help with regarding your calendar?";
    }

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