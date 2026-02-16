namespace CalendarManager.API.Models.DTOs;

public class FreeBusyDto
{
    public string Email { get; set; } = string.Empty;
    public List<BusyPeriod> BusyPeriods { get; set; } = new();
}

public class BusyPeriod
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}