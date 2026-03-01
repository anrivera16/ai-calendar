using CalendarManager.API.Data.Entities;

namespace CalendarManager.API.Models.DTOs;

public class AvailabilityRuleDto
{
    public Guid Id { get; set; }
    public Guid BusinessProfileId { get; set; }
    public RuleType RuleType { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? SpecificDate { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAvailabilityRuleDto
{
    public RuleType RuleType { get; set; }
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public DateTime? SpecificDate { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class CreateWeeklyAvailabilityDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class CreateDateOverrideDto
{
    public DateTime SpecificDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool IsAvailable { get; set; }
}

public class CreateBreakDto
{
    public DateTime SpecificDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
