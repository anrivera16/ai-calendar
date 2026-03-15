using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;
using System.Text.Json.Nodes;
using CommonTool = Anthropic.SDK.Common.Tool;
using AnthropicMessage = Anthropic.SDK.Messaging.Message;
using AnthropicRoleType = Anthropic.SDK.Messaging.RoleType;
using MessageEntity = CalendarManager.API.Data.Entities.Message;

namespace CalendarManager.API.Services.Implementations;

/// <summary>
/// User context passed through method calls instead of stored in instance fields.
/// This ensures thread safety - each request gets its own context.
/// </summary>
public record UserContext(
    Guid UserDbId,
    string UserEmail,
    Guid BusinessProfileId
);

public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _anthropicClient;
    private readonly IGoogleCalendarService _calendarService;
    private readonly IBookingService _bookingService;
    private readonly IAvailabilityService _availabilityService;
    private readonly ILogger<ClaudeService> _logger;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Maximum number of previous messages to include in context
    private const int MaxContextMessages = 20;

    public ClaudeService(
        IConfiguration configuration,
        IGoogleCalendarService calendarService,
        IBookingService bookingService,
        IAvailabilityService availabilityService,
        ILogger<ClaudeService> logger,
        AppDbContext context)
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") 
                    ?? configuration["Claude:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Claude API key not found. Set CLAUDE_API_KEY environment variable or Claude:ApiKey in configuration.");

        _anthropicClient = new AnthropicClient(apiKey);
        _calendarService = calendarService;
        _bookingService = bookingService;
        _availabilityService = availabilityService;
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
                // Try parsing as Guid to properly query by Id
                if (Guid.TryParse(userId, out var userGuid))
                {
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userGuid);
                }
                
                if (user == null)
                {
                    // Also try to find test user by email for demo purposes
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
                }
            }
            
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

            // Get user's business profile
            var businessProfile = await _context.BusinessProfiles
                .FirstOrDefaultAsync(bp => bp.UserId == user.Id);
            var businessProfileId = businessProfile?.Id ?? Guid.Empty;

            // Create user context (thread-safe, passed via parameters)
            var userContext = new UserContext(user.Id, userId, businessProfileId);

            // Parse conversation ID
            Guid? convGuid = null;
            if (!string.IsNullOrEmpty(conversationId) && Guid.TryParse(conversationId, out var parsedConvGuid))
            {
                convGuid = parsedConvGuid;
            }

            // Load conversation history if we have a conversation ID
            List<MessageEntity>? conversationHistory = null;
            if (convGuid.HasValue)
            {
                conversationHistory = await _context.Messages
                    .Where(m => m.ConversationId == convGuid.Value)
                    .OrderBy(m => m.CreatedAt)
                    .Take(MaxContextMessages)
                    .ToListAsync();
            }

            // Process message with AI
            string response;
            List<CalendarAction>? actions = null;
            
            var fallbackOnly = _configuration.GetValue<bool>("Claude:FallbackOnly", false);
            var claudeApiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
                             ?? _configuration["Claude:ApiKey"];
            
            if (fallbackOnly)
            {
                _logger.LogWarning("🔄 FALLBACK MODE ACTIVE: Configuration 'Claude:FallbackOnly' is set to true");
                response = ProcessIntelligentFallback(message);
            }
            else if (string.IsNullOrEmpty(claudeApiKey))
            {
                _logger.LogWarning("🔄 FALLBACK MODE ACTIVE: No Claude API key configured");
                response = ProcessIntelligentFallback(message);
            }
            else
            {
                _logger.LogInformation("🤖 CLAUDE AI MODE: Using Claude API with tool-calling capabilities");
                var result = await ProcessWithClaudeAsync(message, userContext, conversationHistory);
                response = result.Response;
                actions = result.Actions;
            }

            // Save messages to database
            if (convGuid.HasValue)
            {
                // Save user message
                var userMessage = new MessageEntity
                {
                    Id = Guid.NewGuid(),
                    ConversationId = convGuid.Value,
                    Role = "user",
                    Content = message,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(userMessage);

                // Save assistant response
                var assistantMessage = new MessageEntity
                {
                    Id = Guid.NewGuid(),
                    ConversationId = convGuid.Value,
                    Role = "assistant",
                    Content = response,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(assistantMessage);

                // Update conversation timestamp
                var conversation = await _context.Conversations.FindAsync(convGuid.Value);
                if (conversation != null)
                {
                    conversation.UpdatedAt = DateTime.UtcNow;
                    // Set title from first message if not set
                    if (string.IsNullOrEmpty(conversation.Title))
                    {
                        conversation.Title = message.Length > 100 ? message[..100] + "..." : message;
                    }
                }

                await _context.SaveChangesAsync();
            }
            
            return new ChatResponse
            {
                Message = response,
                Success = true,
                Type = MessageType.Success,
                Actions = actions,
                ConversationId = conversationId ?? convGuid?.ToString()
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
                    Success = true,
                    Type = MessageType.Warning,
                    ConversationId = conversationId
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

    private async Task<(string Response, List<CalendarAction>? Actions)> ProcessWithClaudeAsync(
        string message, 
        UserContext userContext,
        List<MessageEntity>? conversationHistory = null)
    {
        try
        {
            _logger.LogInformation("Starting Claude API processing for user: {UserEmail}, BusinessProfileId: {BusinessProfileId}", 
                userContext.UserEmail, userContext.BusinessProfileId);
            
            // Use Claude API with tool calling, passing user context and history
            var result = await CallClaudeAPIAsync(message, userContext, conversationHistory);
            
            _logger.LogInformation("✅ CLAUDE AI SUCCESS: Response generated using Claude API model");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CLAUDE API ERROR: {Error}. Falling back to pattern matching.", ex.Message);
            _logger.LogWarning("🔄 FALLBACK MODE ACTIVATED: Due to Claude API error");
            return (ProcessIntelligentFallback(message), null);
        }
    }

    private async Task<(string Response, List<CalendarAction>? Actions)> CallClaudeAPIAsync(
        string userMessage,
        UserContext userContext,
        List<MessageEntity>? conversationHistory = null)
    {
        try
        {
            _logger.LogInformation("Making Claude API call with tools for message: {Message}", userMessage);
            
            // Build messages list starting with conversation history
            var messages = new List<AnthropicMessage>();

            // Add conversation history if available
            if (conversationHistory != null && conversationHistory.Count > 0)
            {
                foreach (var msg in conversationHistory)
                {
                    var role = msg.Role == "user" ? AnthropicRoleType.User : AnthropicRoleType.Assistant;
                    messages.Add(new AnthropicMessage(role, msg.Content));
                }
                _logger.LogInformation("Added {Count} messages from conversation history", conversationHistory.Count);
            }

            // Add current user message
            messages.Add(new AnthropicMessage(AnthropicRoleType.User, userMessage));

            var tools = GetCalendarTools();
            
            var parameters = new MessageParameters()
            {
                Messages = messages,
                MaxTokens = 4096,
                Model = AnthropicModels.Claude4Sonnet,
                Stream = false,
                Temperature = 0.3m,
                System = new List<SystemMessage>() { new SystemMessage(GetSystemPrompt()) },
                Tools = tools
            };

            _logger.LogInformation("Calling Claude API with model: {Model}, tools: {ToolCount}", parameters.Model, tools.Count);
            
            var executedActions = new List<CalendarAction>();
            const int maxToolLoops = 5;
            var loopCount = 0;

            var response = await _anthropicClient.Messages.GetClaudeMessageAsync(parameters);
            _logger.LogInformation("Claude API response. StopReason: {StopReason}, Content blocks: {Count}", 
                response.StopReason, response.Content.Count);

            // Tool-use loop: keep going while Claude wants to call tools
            while (response.StopReason == "tool_use" && loopCount < maxToolLoops)
            {
                loopCount++;
                _logger.LogInformation("Tool use loop iteration {Loop}", loopCount);

                // Add assistant's response (with tool_use blocks) to conversation
                messages.Add(new AnthropicMessage { Role = AnthropicRoleType.Assistant, Content = response.Content });

                // Process each tool call in the response
                var toolResults = new List<ContentBase>();
                foreach (var toolUse in response.Content.OfType<ToolUseContent>())
                {
                    _logger.LogInformation("Executing tool: {ToolName} (id: {ToolId})", toolUse.Name, toolUse.Id);
                    _logger.LogInformation("Tool input: {Input}", toolUse.Input?.ToJsonString());

                    var toolResult = await ExecuteToolAsync(toolUse, executedActions, userContext);
                    
                    toolResults.Add(new ToolResultContent
                    {
                        ToolUseId = toolUse.Id,
                        Content = new List<ContentBase>
                        {
                            new TextContent { Text = toolResult }
                        }
                    });
                }

                // Add tool results as a user message and call Claude again
                messages.Add(new AnthropicMessage { Role = AnthropicRoleType.User, Content = toolResults });
                
                parameters.Messages = messages;
                response = await _anthropicClient.Messages.GetClaudeMessageAsync(parameters);
                _logger.LogInformation("Claude follow-up response. StopReason: {StopReason}", response.StopReason);
            }

            // Extract final text response
            var textContent = response.Content.OfType<TextContent>().FirstOrDefault();
            var result = textContent?.Text ?? "I processed your request but couldn't generate a response.";
            
            _logger.LogInformation("Final Claude response (length: {Length}): {Response}", result.Length, 
                result.Length > 200 ? result[..200] + "..." : result);
            
            return (result, executedActions.Count > 0 ? executedActions : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API call failed: {Error}", ex.Message);
            
            // Check if it's a credit/billing issue
            if (ex.Message.Contains("credit balance") || ex.Message.Contains("billing") ||
                ex.Message.Contains("payment") || ex.Message.Contains("invalid_request_error"))
            {
                _logger.LogWarning("🔄 FALLBACK MODE ACTIVATED: Claude API unavailable due to billing/credits issue");
                
                var showCreditMessage = _configuration.GetValue<bool>("Claude:ShowCreditMessage", true);
                if (showCreditMessage)
                {
                    return ($"💰 **Claude AI Credits Needed**\n\n" +
                           $"I'm your AI Calendar Assistant! My advanced Claude features need API credits, but I can still provide intelligent help.\n\n" +
                           $"**🤖 Current Capabilities:**\n" +
                           $"• Smart calendar conversation\n" +
                           $"• Scheduling guidance and examples\n" +
                           $"• Time management advice\n" +
                           $"• Natural language understanding\n\n" +
                           $"**🚀 Your request**: \"{userMessage}\"\n\n" +
                           $"Let me help with that:\n\n" +
                           ProcessIntelligentFallback(userMessage) +
                           $"\n\n*💡 Add Claude API credits for advanced AI features like direct calendar creation!*", null);
                }
                
                return (ProcessIntelligentFallback(userMessage), null);
            }
            
            // Check if it's a rate limit issue
            if (ex.Message.Contains("rate_limit") || ex.Message.Contains("429"))
            {
                _logger.LogWarning("🔄 FALLBACK MODE ACTIVATED: Claude API rate limited (HTTP 429)");
                return (ProcessIntelligentFallback(userMessage) + "\n\n*Note: Claude API is temporarily busy. Please try again in a moment.*", null);
            }
            
            // For other errors, provide helpful fallback
            _logger.LogWarning("🔄 FALLBACK MODE ACTIVATED: Claude API error - {Error}", ex.Message);
            return (ProcessIntelligentFallback(userMessage), null);
        }
    }

    /// <summary>
    /// Registers calendar tools that Claude can call during conversation.
    /// Uses CommonTool.FromFunc to create typed tool definitions.
    /// </summary>
    private List<CommonTool> GetCalendarTools()
    {
        var tools = new List<CommonTool>();

        // Tool 1: Create a calendar event
        tools.Add(CommonTool.FromFunc(
            "create_calendar_event",
            (string title, string start_time, string end_time, string? description, string? location) =>
            {
                return "DEFERRED"; // Actual execution happens in ExecuteToolAsync
            },
            "Creates a new event on the user's Google Calendar. Use ISO 8601 format for times (e.g., '2026-02-23T14:00:00'). If the user doesn't specify an end time, default to 1 hour after start. Parse natural language dates relative to the current date/time provided in the system prompt."
        ));

        // Tool 2: List calendar events
        tools.Add(CommonTool.FromFunc(
            "list_calendar_events",
            (string start_date, string end_date, int? max_results) =>
            {
                return "DEFERRED";
            },
            "Lists events from the user's Google Calendar within a date range. Use ISO 8601 format for dates (e.g., '2026-02-23T00:00:00'). For 'today', use today's date from midnight to midnight. For 'this week', use Monday to Sunday."
        ));

        // Tool 3: Delete a calendar event
        tools.Add(CommonTool.FromFunc(
            "delete_calendar_event",
            (string event_id) =>
            {
                return "DEFERRED";
            },
            "Deletes an event from the user's Google Calendar by its event ID. You must first list events to get the event ID before deleting."
        ));

        // Tool 4: Get free/busy information
        tools.Add(CommonTool.FromFunc(
            "get_free_busy",
            (string start_date, string end_date) =>
            {
                return "DEFERRED";
            },
            "Checks the user's availability (free/busy status) for a given time range. Use ISO 8601 format. Returns busy periods so you can identify free slots."
        ));
        
        // Tool 5: Check booking availability
        tools.Add(CommonTool.FromFunc(
            "check_booking_availability",
            (string service_id, string date) =>
            {
                return "DEFERRED";
            },
            "Checks available time slots for booking a service on a specific date. Requires service_id and date in ISO 8601 format (e.g., '2026-02-23'). Returns available slots if any exist."
        ));
        
        // Tool 6: View bookings
        tools.Add(CommonTool.FromFunc(
            "view_bookings",
            (string? start_date, string? end_date, string? status) =>
            {
                return "DEFERRED";
            },
            "Views bookings for the business. Optional parameters: start_date, end_date (ISO 8601), and status (confirmed, cancelled, completed). If no status is provided, returns confirmed bookings by default."
        ));
        
        // Tool 7: Cancel a booking
        tools.Add(CommonTool.FromFunc(
            "cancel_booking",
            (string booking_id) =>
            {
                return "DEFERRED";
            },
            "Cancels a booking by its ID. You must first use view_bookings to get the booking ID. This action cannot be undone."
        ));

        return tools;
    }

    /// <summary>
    /// Executes a tool call from Claude and returns the result as a JSON string.
    /// </summary>
    private async Task<string> ExecuteToolAsync(ToolUseContent toolUse, List<CalendarAction> executedActions, UserContext userContext)
    {
        try
        {
            var input = toolUse.Input;
            
            switch (toolUse.Name)
            {
                case "create_calendar_event":
                    return await ExecuteCreateEventAsync(input, executedActions, userContext);
                    
                case "list_calendar_events":
                    return await ExecuteListEventsAsync(input, executedActions, userContext);
                    
                case "delete_calendar_event":
                    return await ExecuteDeleteEventAsync(input, executedActions, userContext);
                    
                case "get_free_busy":
                    return await ExecuteGetFreeBusyAsync(input, executedActions, userContext);
                    
                case "check_booking_availability":
                    return await ExecuteCheckBookingAvailabilityAsync(input, executedActions, userContext);
                    
                case "view_bookings":
                    return await ExecuteViewBookingsAsync(input, executedActions, userContext);
                    
                case "cancel_booking":
                    return await ExecuteCancelBookingAsync(input, executedActions, userContext);
                    
                default:
                    _logger.LogWarning("Unknown tool: {ToolName}", toolUse.Name);
                    return JsonSerializer.Serialize(new { error = $"Unknown tool: {toolUse.Name}" }, JsonOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}: {Error}", toolUse.Name, ex.Message);
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }

    private async Task<string> ExecuteCreateEventAsync(JsonNode? input, List<CalendarAction> executedActions, UserContext userContext)
    {
        var title = input?["title"]?.GetValue<string>() ?? "Untitled Event";
        var startTimeStr = input?["start_time"]?.GetValue<string>();
        var endTimeStr = input?["end_time"]?.GetValue<string>();
        var description = input?["description"]?.GetValue<string>();
        var location = input?["location"]?.GetValue<string>();

        if (string.IsNullOrEmpty(startTimeStr))
        {
            return JsonSerializer.Serialize(new { error = "start_time is required" }, JsonOptions);
        }

        if (!DateTime.TryParse(startTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startTime))
        {
            return JsonSerializer.Serialize(new { error = $"Could not parse start_time: {startTimeStr}" }, JsonOptions);
        }

        DateTime endTime;
        if (!string.IsNullOrEmpty(endTimeStr) && DateTime.TryParse(endTimeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEnd))
        {
            endTime = parsedEnd;
        }
        else
        {
            endTime = startTime.AddHours(1); // Default 1 hour duration
        }

        _logger.LogInformation("Creating event: {Title} from {Start} to {End}", title, startTime, endTime);

        var createDto = new CreateEventDto
        {
            Title = title,
            Start = startTime,
            End = endTime,
            Description = description,
            Location = location
        };

        var createdEvent = await _calendarService.CreateEventAsync(userContext.UserDbId, createDto);
        
        var action = new CalendarAction
        {
            Type = "create_calendar_event",
            Data = createdEvent,
            Executed = true,
            Result = $"Created event '{title}' on {startTime:MM/dd/yyyy} at {startTime:h:mm tt}"
        };
        executedActions.Add(action);

        return JsonSerializer.Serialize(new
        {
            success = true,
            eventId = createdEvent.Id,
            title = createdEvent.Title,
            start = createdEvent.Start,
            end = createdEvent.End,
            location = createdEvent.Location,
            message = $"Successfully created event '{title}'"
        }, JsonOptions);
    }

    private async Task<string> ExecuteListEventsAsync(JsonNode? input, List<CalendarAction> executedActions, UserContext userContext)
    {
        var startDateStr = input?["start_date"]?.GetValue<string>();
        var endDateStr = input?["end_date"]?.GetValue<string>();
        var maxResults = 10;
        
        try
        {
            var maxResultsNode = input?["max_results"];
            if (maxResultsNode != null)
                maxResults = maxResultsNode.GetValue<int>();
        }
        catch { /* use default */ }

        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(7);

        if (!string.IsNullOrEmpty(startDateStr) && DateTime.TryParse(startDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStart))
        {
            startDate = parsedStart;
        }
        if (!string.IsNullOrEmpty(endDateStr) && DateTime.TryParse(endDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEnd))
        {
            endDate = parsedEnd;
        }

        _logger.LogInformation("Listing events from {Start} to {End} (max: {Max})", startDate, endDate, maxResults);

        var events = await _calendarService.GetEventsAsync(userContext.UserDbId, startDate, endDate);
        var limitedEvents = events.Take(maxResults).ToList();

        var action = new CalendarAction
        {
            Type = "list_calendar_events",
            Data = limitedEvents,
            Executed = true,
            Result = $"Found {limitedEvents.Count} events between {startDate:MM/dd} and {endDate:MM/dd}"
        };
        executedActions.Add(action);

        var eventList = limitedEvents.Select(e => new
        {
            id = e.Id,
            title = e.Title,
            start = e.Start,
            end = e.End,
            location = e.Location,
            description = e.Description
        }).ToList();

        return JsonSerializer.Serialize(new
        {
            success = true,
            totalEvents = limitedEvents.Count,
            events = eventList,
            dateRange = new { start = startDate, end = endDate }
        }, JsonOptions);
    }

    private async Task<string> ExecuteDeleteEventAsync(JsonNode? input, List<CalendarAction> executedActions, UserContext userContext)
    {
        var eventId = input?["event_id"]?.GetValue<string>();
        
        if (string.IsNullOrEmpty(eventId))
        {
            return JsonSerializer.Serialize(new { error = "event_id is required" }, JsonOptions);
        }

        _logger.LogInformation("Deleting event: {EventId}", eventId);

        await _calendarService.DeleteEventAsync(userContext.UserDbId, eventId);

        var action = new CalendarAction
        {
            Type = "delete_calendar_event",
            Executed = true,
            Result = $"Deleted event {eventId}"
        };
        executedActions.Add(action);

        return JsonSerializer.Serialize(new
        {
            success = true,
            deletedEventId = eventId,
            message = "Event successfully deleted"
        }, JsonOptions);
    }

    private async Task<string> ExecuteGetFreeBusyAsync(JsonNode? input, List<CalendarAction> executedActions, UserContext userContext)
    {
        var startDateStr = input?["start_date"]?.GetValue<string>();
        var endDateStr = input?["end_date"]?.GetValue<string>();

        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(1);

        if (!string.IsNullOrEmpty(startDateStr) && DateTime.TryParse(startDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStart))
        {
            startDate = parsedStart;
        }
        if (!string.IsNullOrEmpty(endDateStr) && DateTime.TryParse(endDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEnd))
        {
            endDate = parsedEnd;
        }

        _logger.LogInformation("Checking free/busy from {Start} to {End}", startDate, endDate);

        var freeBusy = await _calendarService.GetFreeBusyAsync(userContext.UserDbId, userContext.UserEmail, startDate, endDate);

        var action = new CalendarAction
        {
            Type = "get_free_busy",
            Data = freeBusy,
            Executed = true,
            Result = $"Found {freeBusy.BusyPeriods.Count} busy periods between {startDate:MM/dd} and {endDate:MM/dd}"
        };
        executedActions.Add(action);

        return JsonSerializer.Serialize(new
        {
            success = true,
            busyPeriods = freeBusy.BusyPeriods.Select(bp => new
            {
                start = bp.Start,
                end = bp.End
            }).ToList(),
            totalBusyPeriods = freeBusy.BusyPeriods.Count,
            dateRange = new { start = startDate, end = endDate }
        }, JsonOptions);
    }
    
    private async Task<string> ExecuteCheckBookingAvailabilityAsync(JsonNode? input, List<CalendarAction> executedActions, UserContext userContext)
    {
        if (userContext.BusinessProfileId == Guid.Empty)
        {
            return JsonSerializer.Serialize(new { error = "No business profile found for this user" }, JsonOptions);
        }
        
        var serviceIdStr = input?["service_id"]?.GetValue<string>();
        var dateStr = input?["date"]?.GetValue<string>();
        
        if (string.IsNullOrEmpty(serviceIdStr) || string.IsNullOrEmpty(dateStr))
        {
            return JsonSerializer.Serialize(new { error = "service_id and date are required" }, JsonOptions);
        }
        
        if (!Guid.TryParse(serviceIdStr, out var serviceId))
        {
            return JsonSerializer.Serialize(new { error = "Invalid service_id format" }, JsonOptions);
        }
        
        if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return JsonSerializer.Serialize(new { error = $"Could not parse date: {dateStr}" }, JsonOptions);
        }
        
        _logger.LogInformation("Checking booking availability for service {ServiceId} on {Date}", serviceId, date);
        
        var availableSlots = await _availabilityService.GetAvailableSlotsAsync(userContext.BusinessProfileId, serviceId, date);
        
        var action = new CalendarAction
        {
            Type = "check_booking_availability",
            Executed = true,
            Result = $"Found {availableSlots.Count} available slots for {date:MM/dd/yyyy}"
        };
        executedActions.Add(action);
        
        return JsonSerializer.Serialize(new
        {
            success = true,
            serviceId = serviceId,
            date = date,
            availableSlots = availableSlots.Select(s => new
            {
                startTime = s.StartTime,
                endTime = s.EndTime
            }).ToList(),
            totalSlots = availableSlots.Count
        }, JsonOptions);
    }
    
    private async Task<string> ExecuteViewBookingsAsync(JsonNode? input, List<CalendarAction> executedActions, UserContext userContext)
    {
        if (userContext.BusinessProfileId == Guid.Empty)
        {
            return JsonSerializer.Serialize(new { error = "No business profile found for this user" }, JsonOptions);
        }
        
        var startDateStr = input?["start_date"]?.GetValue<string>();
        var endDateStr = input?["end_date"]?.GetValue<string>();
        var statusStr = input?["status"]?.GetValue<string>();
        
        DateTime? startDate = null;
        DateTime? endDate = null;
        
        if (!string.IsNullOrEmpty(startDateStr) && DateTime.TryParse(startDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedStart))
        {
            startDate = parsedStart;
        }
        if (!string.IsNullOrEmpty(endDateStr) && DateTime.TryParse(endDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedEnd))
        {
            endDate = parsedEnd;
        }
        
        _logger.LogInformation("Viewing bookings for business {BusinessProfileId} from {Start} to {End}, status: {Status}", 
            userContext.BusinessProfileId, startDate, endDate, statusStr);
        
        var bookings = await _bookingService.GetBookingsAsync(userContext.BusinessProfileId, startDate, endDate);
        
        // Filter by status if provided
        if (!string.IsNullOrEmpty(statusStr) && Enum.TryParse<BookingStatus>(statusStr, true, out var status))
        {
            bookings = bookings.Where(b => b.Status == status).ToList();
        }
        
        var action = new CalendarAction
        {
            Type = "view_bookings",
            Executed = true,
            Result = $"Found {bookings.Count} bookings"
        };
        executedActions.Add(action);
        
        return JsonSerializer.Serialize(new
        {
            success = true,
            bookings = bookings.Select(b => new
            {
                id = b.Id,
                serviceName = b.Service?.Name,
                clientName = b.Client?.Name,
                clientEmail = b.Client?.Email,
                startTime = b.StartTime,
                endTime = b.EndTime,
                status = b.Status.ToString(),
                notes = b.Notes
            }).ToList(),
            totalBookings = bookings.Count
        }, JsonOptions);
    }
    
    private async Task<string> ExecuteCancelBookingAsync(JsonNode? input, List<CalendarAction> executedActions, UserContext userContext)
    {
        if (userContext.BusinessProfileId == Guid.Empty)
        {
            return JsonSerializer.Serialize(new { error = "No business profile found for this user" }, JsonOptions);
        }
        
        var bookingIdStr = input?["booking_id"]?.GetValue<string>();
        
        if (string.IsNullOrEmpty(bookingIdStr))
        {
            return JsonSerializer.Serialize(new { error = "booking_id is required" }, JsonOptions);
        }
        
        if (!Guid.TryParse(bookingIdStr, out var bookingId))
        {
            return JsonSerializer.Serialize(new { error = "Invalid booking_id format" }, JsonOptions);
        }
        
        _logger.LogInformation("Cancelling booking {BookingId} for business {BusinessProfileId}", bookingId, userContext.BusinessProfileId);
        
        try
        {
            var cancelledBooking = await _bookingService.CancelBookingAsync(userContext.BusinessProfileId, bookingId);
            
            var action = new CalendarAction
            {
                Type = "cancel_booking",
                Executed = true,
                Result = $"Cancelled booking for {cancelledBooking.Service?.Name} on {cancelledBooking.StartTime:MM/dd/yyyy}"
            };
            executedActions.Add(action);
            
            return JsonSerializer.Serialize(new
            {
                success = true,
                cancelledBooking = new
                {
                    id = cancelledBooking.Id,
                    serviceName = cancelledBooking.Service?.Name,
                    clientName = cancelledBooking.Client?.Name,
                    startTime = cancelledBooking.StartTime,
                    status = cancelledBooking.Status.ToString()
                },
                message = "Booking successfully cancelled"
            }, JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, JsonOptions);
        }
    }

    private string ProcessIntelligentFallback(string message)
    {
        _logger.LogInformation("📝 PROCESSING WITH FALLBACK: Using pattern-matching intelligent fallback for message: {Message}", message);
        
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
               $"*🔄 AI fallback mode — Claude API may be temporarily unavailable.*";
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
               $"*🔄 AI fallback mode — Claude API may be temporarily unavailable.*";
    }

    private string ProcessCancellationRequest(string originalMessage, string lowerMessage)
    {
        return $"🗑️ **Event Management**\n\n" +
               $"I understand you want to cancel, delete, or modify an event.\n\n" +
               $"**For now, you can:**\n" +
               $"• Go directly to Google Calendar to modify events\n" +
               $"• Use your calendar app to make changes\n" +
               $"• Let me know the specific event and I'll guide you\n\n" +
               $"*🔄 AI fallback mode — Claude API may be temporarily unavailable.*";
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
               $"*🔄 AI fallback mode — Claude API may be temporarily unavailable.*";
    }

    private string ProcessHelpRequest(string originalMessage, DateTime currentTime)
    {
        return $"🤖 **AI Calendar Assistant**\n\n" +
               $"I'm your intelligent calendar companion! Here's what I can help with:\n\n" +
               $"**✅ Available Features:**\n" +
               $"• Create calendar events from natural language\n" +
               $"• View your upcoming events and schedule\n" +
               $"• Check your availability / free time\n" +
               $"• Delete or manage events\n\n" +
               $"**💡 Try asking:**\n" +
               $"• \"Schedule a team meeting tomorrow at 2pm\"\n" +
               $"• \"What's on my calendar today?\"\n" +
               $"• \"When am I free this week?\"\n" +
               $"• \"Delete my 3pm meeting\"\n\n" +
               $"*🔄 AI fallback mode — Claude API may be temporarily unavailable.*";
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
               $"I'm here to help with your calendar and scheduling needs:\n\n" +
               $"• **Creating events**: \"Schedule lunch with John tomorrow\"\n" +
               $"• **Viewing schedules**: \"What's on my calendar today?\"\n" +
               $"• **Time management**: \"When am I free this week?\"\n" +
               $"• **Planning**: \"Help me find time for a 2-hour meeting\"\n\n" +
                $"**Current time**: {currentTime:h:mm tt} on {currentTime:dddd, MMMM d}\n\n" +
                $"What would you like help with regarding your calendar?";
    }

    private string GetSystemPrompt()
    {
        return @"You are an AI calendar assistant that helps business owners manage their Google Calendar AND their booking platform through natural language.

## Your Capabilities

### Google Calendar Tools:
- create_calendar_event: Create new calendar events
- list_calendar_events: View existing events  
- delete_calendar_event: Remove events (requires event ID — list events first)
- get_free_busy: Check availability

### Booking Management Tools (for business owners):
- check_booking_availability: Check available time slots for booking a service on a specific date
- view_bookings: View all bookings for the business, optionally filtered by date range and status
- cancel_booking: Cancel a booking by its ID (requires booking ID — view bookings first)

IMPORTANT RULES:
1. Current date and time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss (dddd)") + @"
2. When the user asks to schedule/create/book something, USE the create_calendar_event tool immediately. Do NOT just describe what you would do.
3. When the user asks to see/view/show events, USE the list_calendar_events tool. Do NOT tell them to check their calendar manually.
4. When the user asks about availability or free time, USE the get_free_busy tool.
5. When the user asks to cancel/delete an event, first USE list_calendar_events to find the event ID, then USE delete_calendar_event.
6. When the user asks about bookings (e.g., 'show me bookings', 'what appointments do I have', 'view reservations'), USE the view_bookings tool.
7. When the user asks about available slots for clients, USE check_booking_availability.
8. When the user asks to cancel a booking/appointment, first USE view_bookings to find the booking ID, then USE cancel_booking.
9. Parse natural language dates relative to the current date above. 'Tomorrow' means " + DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") + @". 'Next Monday' means the coming Monday, etc.
10. Default event duration is 1 hour if not specified.
11. Use ISO 8601 format for all dates/times passed to tools.
12. After executing a tool, summarize what you did in a friendly, concise way.
13. If a tool call fails, explain the error clearly and suggest alternatives.
14. Be conversational but efficient. Don't over-explain — just do what the user asks.
15. When discussing bookings, mention the client name, service, date, time, and status (confirmed, cancelled, completed).";
    }

    // Implementation of interface methods
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
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userId);
            if (user == null)
            {
                return new CalendarAction
                {
                    Type = "find_free_time",
                    Executed = false,
                    ErrorMessage = "User not found"
                };
            }

            var freeBusy = await _calendarService.GetFreeBusyAsync(
                user.Id, userId, toolCall.StartDate, toolCall.EndDate);

            return new CalendarAction
            {
                Type = "find_free_time",
                Data = freeBusy,
                Executed = true,
                Result = $"Found {freeBusy.BusyPeriods.Count} busy periods. Free time available outside those periods."
            };
        }
        catch (Exception ex)
        {
            return new CalendarAction
            {
                Type = "find_free_time",
                Executed = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
