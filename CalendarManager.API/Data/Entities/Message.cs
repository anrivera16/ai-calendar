using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Data.Entities;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ConversationId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty; // 'user' or 'assistant'
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string? FunctionCalls { get; set; } // JSON for Claude function calls
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Conversation Conversation { get; set; } = null!;
}