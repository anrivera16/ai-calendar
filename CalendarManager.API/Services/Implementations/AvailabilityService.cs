using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CalendarManager.API.Services.Implementations;

public class AvailabilityService : IAvailabilityService
{
    private readonly AppDbContext _context;
    private readonly IGoogleCalendarService _calendarService;
    private readonly ILogger<AvailabilityService> _logger;
    
    public AvailabilityService(AppDbContext context, IGoogleCalendarService calendarService, ILogger<AvailabilityService> logger)
    {
        _context = context;
        _calendarService = calendarService;
        _logger = logger;
    }
    
    public async Task<List<AvailableSlotDto>> GetAvailableSlotsAsync(Guid businessProfileId, Guid serviceId, DateTime date)
    {
        // Get the service to determine duration
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null || service.BusinessProfileId != businessProfileId)
        {
            return new List<AvailableSlotDto>();
        }
        
        var durationMinutes = service.DurationMinutes;
        
        // Get business profile to find the user (for Google Calendar)
        var businessProfile = await _context.BusinessProfiles
            .Include(bp => bp.User)
            .FirstOrDefaultAsync(bp => bp.Id == businessProfileId);
        
        if (businessProfile == null)
        {
            return new List<AvailableSlotDto>();
        }
        
        // Get availability rules for this date
        var rules = await GetRulesForDateAsync(businessProfileId, date);
        
        // If no rules exist for this date, return empty
        if (!rules.Any())
        {
            return new List<AvailableSlotDto>();
        }
        
        // Get existing bookings for this date
        var startOfDay = date.Date;
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);
        
        var existingBookings = await _context.Bookings
            .Where(b => b.BusinessProfileId == businessProfileId
                     && b.Status == BookingStatus.Confirmed
                     && b.StartTime >= startOfDay
                     && b.StartTime < endOfDay)
            .ToListAsync();
        
        // Get Google Calendar busy times
        var busyTimes = new List<(DateTime Start, DateTime End)>();
        
        if (businessProfile.UserId != Guid.Empty)
        {
            try
            {
                var googleEvents = await _calendarService.GetEventsAsync(
                    businessProfile.UserId,
                    startOfDay,
                    endOfDay);
                
                // Filter out events with invalid or default start/end times
                busyTimes.AddRange(googleEvents
                    .Where(e => e.Start > DateTime.MinValue && e.End > DateTime.MinValue)
                    .Select(e => (e.Start, e.End)));
            }
            catch (Exception ex)
            {
                // If Google Calendar fails, continue with just DB bookings but log the error
                _logger.LogWarning(ex, "Failed to fetch Google Calendar events for business {BusinessProfileId} on {Date}. Using database bookings only.", businessProfileId, date);
            }
        }
        
        // Add existing bookings to busy times
        busyTimes.AddRange(existingBookings.Select(b => (b.StartTime, b.EndTime)));
        
        // Calculate available slots
        var availableSlots = new List<AvailableSlotDto>();
        
        foreach (var rule in rules.Where(r => r.IsAvailable).OrderBy(r => r.StartTime))
        {
            if (!rule.StartTime.HasValue || !rule.EndTime.HasValue)
            {
                continue;
            }
            
            var slotStart = date.Date.Add(rule.StartTime.Value);
            var slotEnd = date.Date.Add(rule.EndTime.Value);
            var currentSlot = slotStart;
            
            while (currentSlot.AddMinutes(durationMinutes) <= slotEnd)
            {
                var slotEndTime = currentSlot.AddMinutes(durationMinutes);
                
                // Check if this slot overlaps with any busy time
                var isOverlapping = busyTimes.Any(bt =>
                    (currentSlot >= bt.Start && currentSlot < bt.End) ||
                    (slotEndTime > bt.Start && slotEndTime <= bt.End) ||
                    (currentSlot <= bt.Start && slotEndTime >= bt.End));
                
                if (!isOverlapping)
                {
                    availableSlots.Add(new AvailableSlotDto
                    {
                        StartTime = currentSlot,
                        EndTime = slotEndTime
                    });
                }
                
                currentSlot = currentSlot.AddMinutes(30); // 30-minute intervals
            }
        }
        
        return availableSlots.OrderBy(s => s.StartTime).ToList();
    }
    
    public async Task<List<AvailabilityRuleDto>> GetRulesForDateAsync(Guid businessProfileId, DateTime date)
    {
        var rules = await _context.AvailabilityRules
            .Where(r => r.BusinessProfileId == businessProfileId)
            .ToListAsync();
        
        // Get rules that apply to this date
        var applicableRules = rules.Where(r =>
            (r.RuleType == RuleType.DateOverride && r.SpecificDate?.Date == date.Date) ||
            (r.RuleType == RuleType.Weekly && r.DayOfWeek == date.DayOfWeek) ||
            (r.RuleType == RuleType.Break && r.SpecificDate?.Date == date.Date))
            .ToList();
        
        // If no specific rules, check for default weekly hours
        if (!applicableRules.Any())
        {
            var weeklyRules = rules.Where(r =>
                r.RuleType == RuleType.Weekly && r.DayOfWeek == date.DayOfWeek).ToList();
            
            if (weeklyRules.Any())
            {
                applicableRules = weeklyRules;
            }
        }
        
        return applicableRules.Select(MapToDto).ToList();
    }
    
    private static AvailabilityRuleDto MapToDto(AvailabilityRule rule)
    {
        return new AvailabilityRuleDto
        {
            Id = rule.Id,
            BusinessProfileId = rule.BusinessProfileId,
            RuleType = rule.RuleType,
            DayOfWeek = rule.DayOfWeek,
            StartTime = rule.StartTime,
            EndTime = rule.EndTime,
            SpecificDate = rule.SpecificDate,
            IsAvailable = rule.IsAvailable,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}
