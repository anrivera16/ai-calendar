namespace CalendarManager.API.Models.DTOs;

public class AvailableSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class GetAvailableSlotsQuery
{
    public Guid ServiceId { get; set; }
    public DateTime Date { get; set; }
}

public class BusinessPublicDto
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public List<ServiceDto> Services { get; set; } = new();
}
