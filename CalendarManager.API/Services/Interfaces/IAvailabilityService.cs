using CalendarManager.API.Models.DTOs;

namespace CalendarManager.API.Services.Interfaces;

public interface IAvailabilityService
{
    /// <summary>
    /// Gets available time slots for a service on a specific date
    /// </summary>
    /// <param name="businessProfileId">Business profile ID</param>
    /// <param name="serviceId">Service ID</param>
    /// <param name="date">Date to check availability</param>
    /// <returns>List of available time slots</returns>
    Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(Guid businessProfileId, Guid serviceId, DateTime date);
    
    /// <summary>
    /// Gets business rules for availability calculation
    /// </summary>
    /// <param name="businessProfileId">Business profile ID</param>
    /// <param name="date">Specific date</param>
    /// <returns>List of availability rules for the date</returns>
    Task<List<AvailabilityRuleDto>> GetRulesForDateAsync(Guid businessProfileId, DateTime date);
}
