using CalendarManager.API.Models.DTOs;

namespace CalendarManager.API.Services.Interfaces;

public interface IGoogleCalendarService
{
    /// <summary>
    /// Gets calendar events for a date range
    /// </summary>
    /// <param name="userId">User ID for authentication</param>
    /// <param name="start">Start date for event search</param>
    /// <param name="end">End date for event search</param>
    /// <returns>List of calendar events</returns>
    Task<List<CalendarEventDto>> GetEventsAsync(Guid userId, DateTime start, DateTime end);
    
    /// <summary>
    /// Creates a new calendar event
    /// </summary>
    /// <param name="userId">User ID for authentication</param>
    /// <param name="request">Event creation request</param>
    /// <returns>Created calendar event</returns>
    Task<CalendarEventDto> CreateEventAsync(Guid userId, CreateEventDto request);
    
    /// <summary>
    /// Updates an existing calendar event
    /// </summary>
    /// <param name="userId">User ID for authentication</param>
    /// <param name="eventId">Google Calendar event ID</param>
    /// <param name="request">Event update request</param>
    /// <returns>Updated calendar event</returns>
    Task<CalendarEventDto> UpdateEventAsync(Guid userId, string eventId, UpdateEventDto request);
    
    /// <summary>
    /// Deletes a calendar event
    /// </summary>
    /// <param name="userId">User ID for authentication</param>
    /// <param name="eventId">Google Calendar event ID</param>
    Task DeleteEventAsync(Guid userId, string eventId);
    
    /// <summary>
    /// Gets free/busy information for an email address
    /// </summary>
    /// <param name="userId">User ID for authentication</param>
    /// <param name="email">Email address to check availability for</param>
    /// <param name="start">Start date for availability check</param>
    /// <param name="end">End date for availability check</param>
    /// <returns>Free/busy information</returns>
    Task<FreeBusyDto> GetFreeBusyAsync(Guid userId, string email, DateTime start, DateTime end);
    
    /// <summary>
    /// Validates if the current access token is still valid
    /// </summary>
    /// <param name="userId">User ID for authentication</param>
    /// <returns>True if token is valid, false otherwise</returns>
    Task<bool> ValidateTokenAsync(Guid userId);
}