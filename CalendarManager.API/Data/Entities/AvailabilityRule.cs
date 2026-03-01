using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Data.Entities;

public enum RuleType
{
    Weekly,
    DateOverride,
    Break
}

public class AvailabilityRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid BusinessProfileId { get; set; }
    
    public RuleType RuleType { get; set; }
    
    public DayOfWeek? DayOfWeek { get; set; }
    
    public TimeSpan? StartTime { get; set; }
    
    public TimeSpan? EndTime { get; set; }
    
    public DateTime? SpecificDate { get; set; }
    
    public bool IsAvailable { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public virtual BusinessProfile BusinessProfile { get; set; } = null!;
}
