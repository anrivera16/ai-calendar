namespace CalendarManager.API.Models.DTOs;

public class UpdateEventDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public string? Location { get; set; }
    public List<string>? Attendees { get; set; }
    public bool? SendNotifications { get; set; } = true;
}