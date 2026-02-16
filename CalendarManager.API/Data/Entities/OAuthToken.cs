using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Data.Entities;

public class OAuthToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [Required]
    public string AccessToken { get; set; } = string.Empty; // Encrypted
    
    [Required]
    public string RefreshToken { get; set; } = string.Empty; // Encrypted
    
    public DateTime ExpiresAt { get; set; }
    
    public string? Scope { get; set; }
    
    [MaxLength(50)]
    public string TokenType { get; set; } = "Bearer";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;
}