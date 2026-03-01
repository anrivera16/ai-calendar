using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Data.Entities;

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid BusinessProfileId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    public int DurationMinutes { get; set; }
    
    public decimal Price { get; set; }
    
    [MaxLength(7)]
    public string Color { get; set; } = "#3B82F6"; // Default blue
    
    public bool IsActive { get; set; } = true;
    
    public int SortOrder { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual BusinessProfile BusinessProfile { get; set; } = null!;
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
