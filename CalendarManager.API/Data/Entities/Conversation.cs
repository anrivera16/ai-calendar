using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Data.Entities;

public class Conversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [MaxLength(500)]
    public string? Title { get; set; } // Auto-generated from first message
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}