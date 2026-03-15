using System.Text.Json.Serialization;

namespace CalendarManager.API.Models;

/// <summary>
/// Represents user profile information from Google's userinfo endpoint
/// </summary>
public class GoogleUserInfo
{
    /// <summary>
    /// The user's unique Google ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's email address
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's display name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    /// <summary>
    /// URL to the user's profile picture
    /// </summary>
    [JsonPropertyName("picture")]
    public string? Picture { get; set; }
    
    /// <summary>
    /// Whether the email has been verified
    /// </summary>
    [JsonPropertyName("verified_email")]
    public bool VerifiedEmail { get; set; }
}
