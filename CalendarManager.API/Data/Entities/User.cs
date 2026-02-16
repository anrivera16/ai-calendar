using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Data.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(255)]
    public string? GoogleUserId { get; set; }
    
    [MaxLength(255)]
    public string? DisplayName { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLogin { get; set; }

    // Navigation properties
    public virtual ICollection<OAuthToken> OAuthTokens { get; set; } = new List<OAuthToken>();
    public virtual UserPreferences? UserPreferences { get; set; }
    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}