using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Data.Entities;

public class UserPreferences
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    public TimeOnly WorkingHoursStart { get; set; } = new TimeOnly(9, 0); // 09:00
    
    public TimeOnly WorkingHoursEnd { get; set; } = new TimeOnly(17, 0); // 17:00
    
    public string WorkingDays { get; set; } = """["Monday","Tuesday","Wednesday","Thursday","Friday"]"""; // JSON array
    
    public int MeetingBufferMinutes { get; set; } = 15;
    
    public int PreferredMeetingDuration { get; set; } = 30;
    
    [MaxLength(50)]
    public string Timezone { get; set; } = "America/Chicago";
    
    public string? PreferencesText { get; set; } // Natural language preferences
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}