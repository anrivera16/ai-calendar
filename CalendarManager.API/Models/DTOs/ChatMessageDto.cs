namespace CalendarManager.API.Models.DTOs;

public class ChatMessageRequest
{
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? UserEmail { get; set; } // UserEmail is now provided by JWT auth, not request body
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public List<CalendarAction>? Actions { get; set; }
    public string? ConversationId { get; set; }
    public MessageType Type { get; set; } = MessageType.Info;
}

public class CalendarAction
{
    public string Type { get; set; } = string.Empty; // "create_event", "list_events", etc.
    public object? Data { get; set; }
    public bool Executed { get; set; }
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum MessageType
{
    Info,
    Success,
    Warning,
    Error
}

// Tool call result classes for Claude integration
public class CreateEventToolCall
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public List<string>? Attendees { get; set; }
}

public class ListEventsToolCall
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxResults { get; set; } = 10;
}

public class FindFreeTimeToolCall
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DurationMinutes { get; set; }
    public string? PreferredTimeStart { get; set; } = "09:00";
    public string? PreferredTimeEnd { get; set; } = "17:00";
}