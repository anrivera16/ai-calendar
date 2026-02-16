namespace CalendarManager.API.Models.DTOs;

public class CalendarEventDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Location { get; set; }
    public List<string> Attendees { get; set; } = new();
    public string? HtmlLink { get; set; }
    public bool IsAllDay { get; set; }
    public string? Status { get; set; } // confirmed, tentative, cancelled
}