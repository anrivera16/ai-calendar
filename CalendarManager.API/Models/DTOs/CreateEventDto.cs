using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Models.DTOs;

public class CreateEventDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    public DateTime Start { get; set; }
    
    [Required]
    public DateTime End { get; set; }
    
    public string? Location { get; set; }
    
    public List<string> Attendees { get; set; } = new();
    
    public bool IsAllDay { get; set; }
    
    public bool SendNotifications { get; set; } = true;
}