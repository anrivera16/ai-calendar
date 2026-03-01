using System.ComponentModel.DataAnnotations;
using CalendarManager.API.Data.Entities;

namespace CalendarManager.API.Models.DTOs;

public class BookingDto
{
    public Guid Id { get; set; }
    public Guid BusinessProfileId { get; set; }
    public Guid ServiceId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public BookingStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? GoogleCalendarEventId { get; set; }
    public Guid CancellationToken { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Related data
    public ServiceDto? Service { get; set; }
    public ClientDto? Client { get; set; }
}

public class ClientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBookingDto
{
    public Guid ServiceId { get; set; }
    public DateTime StartTime { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string ClientName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string ClientEmail { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public string? Notes { get; set; }
}

public class BookingConfirmationDto
{
    public BookingDto Booking { get; set; } = null!;
    public string ManagementUrl { get; set; } = string.Empty;
    public BusinessProfileDto Business { get; set; } = null!;
}
