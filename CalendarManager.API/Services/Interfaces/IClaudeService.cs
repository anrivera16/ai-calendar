using CalendarManager.API.Models.DTOs;

namespace CalendarManager.API.Services.Interfaces;

public interface IClaudeService
{
    Task<ChatResponse> ProcessMessageAsync(string message, string userId, string? conversationId = null);
    
    // Tool execution methods
    Task<CalendarAction> CreateCalendarEventAsync(CreateEventToolCall toolCall, string userId);
    Task<CalendarAction> ListCalendarEventsAsync(ListEventsToolCall toolCall, string userId);
    Task<CalendarAction> FindFreeTimeAsync(FindFreeTimeToolCall toolCall, string userId);
}