using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Data.Entities;

public enum BookingStatus
{
    Confirmed,
    Cancelled,
    Completed,
    NoShow
}

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid BusinessProfileId { get; set; }
    
    public Guid ServiceId { get; set; }
    
    public Guid ClientId { get; set; }
    
    public DateTime StartTime { get; set; }
    
    public DateTime EndTime { get; set; }
    
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    
    [MaxLength(2000)]
    public string? Notes { get; set; }
    
    [MaxLength(255)]
    public string? GoogleCalendarEventId { get; set; }
    
    [Required]
    public Guid CancellationToken { get; set; } = Guid.NewGuid();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual BusinessProfile BusinessProfile { get; set; } = null!;
    public virtual Service Service { get; set; } = null!;
    public virtual Client Client { get; set; } = null!;
}
