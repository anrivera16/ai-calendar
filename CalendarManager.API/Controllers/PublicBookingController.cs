using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PublicBookingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAvailabilityService _availabilityService;
    private readonly IBookingService _bookingService;

    public PublicBookingController(
        AppDbContext context,
        IAvailabilityService availabilityService,
        IBookingService bookingService)
    {
        _context = context;
        _availabilityService = availabilityService;
        _bookingService = bookingService;
    }

    // GET /api/book/{slug}
    // Returns business info + services
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBusinessBySlug(string slug)
    {
        var business = await _context.BusinessProfiles
            .Include(bp => bp.User)
            .FirstOrDefaultAsync(bp => bp.Slug == slug && bp.IsActive);

        if (business == null)
        {
            return NotFound(new { error = "Business not found" });
        }

        var services = await _context.Services
            .Where(s => s.BusinessProfileId == business.Id && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return Ok(new
        {
            id = business.Id,
            businessName = business.BusinessName,
            description = business.Description,
            logoUrl = business.LogoUrl,
            phone = business.Phone,
            website = business.Website,
            address = business.Address,
            services = services.Select(s => new
            {
                id = s.Id,
                name = s.Name,
                description = s.Description,
                durationMinutes = s.DurationMinutes,
                price = s.Price,
                color = s.Color
            }).ToList()
        });
    }

    // GET /api/book/{slug}/slots?serviceId=&date=
    // Returns available time slots
    [HttpGet("{slug}/slots")]
    public async Task<IActionResult> GetAvailableSlots(
        string slug,
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime date)
    {
        var business = await _context.BusinessProfiles
            .FirstOrDefaultAsync(bp => bp.Slug == slug && bp.IsActive);

        if (business == null)
        {
            return NotFound(new { error = "Business not found" });
        }

        // Validate service exists
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.BusinessProfileId == business.Id);

        if (service == null)
        {
            return NotFound(new { error = "Service not found" });
        }

        var slots = await _availabilityService.GetAvailableSlotsAsync(
            business.Id,
            serviceId,
            date.Date);

        return Ok(new
        {
            date = date.Date,
            serviceId = serviceId,
            durationMinutes = service.DurationMinutes,
            slots = slots.Select(s => new
            {
                startTime = s.StartTime,
                endTime = s.EndTime
            }).ToList()
        });
    }

    // POST /api/book/{slug}
    // Create a new booking
    [HttpPost("{slug}")]
    public async Task<IActionResult> CreateBooking(string slug, [FromBody] CreateBookingRequest request)
    {
        var business = await _context.BusinessProfiles
            .FirstOrDefaultAsync(bp => bp.Slug == slug && bp.IsActive);

        if (business == null)
        {
            return NotFound(new { error = "Business not found" });
        }

        try
        {
            var createBooking = new Models.DTOs.CreateBookingDto
            {
                ServiceId = request.ServiceId,
                StartTime = request.StartTime,
                ClientName = request.ClientName,
                ClientEmail = request.ClientEmail,
                ClientPhone = request.ClientPhone,
                Notes = request.Notes
            };

            var confirmation = await _bookingService.CreateBookingAsync(business.Id, createBooking);

            return Ok(new
            {
                booking = new
                {
                    id = confirmation.Booking.Id,
                    serviceName = confirmation.Booking.Service?.Name,
                    startTime = confirmation.Booking.StartTime,
                    endTime = confirmation.Booking.EndTime,
                    status = confirmation.Booking.Status.ToString(),
                    client = confirmation.Booking.Client
                },
                business = new
                {
                    name = confirmation.Business.BusinessName,
                    address = confirmation.Business.Address,
                    phone = confirmation.Business.Phone
                },
                managementUrl = confirmation.ManagementUrl,
                message = "Booking confirmed successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to create booking", details = ex.Message });
        }
    }

    // GET /api/book/manage/{cancellationToken}
    // View booking details
    [HttpGet("manage/{cancellationToken:guid}")]
    public async Task<IActionResult> GetBookingByToken(Guid cancellationToken)
    {
        var booking = await _bookingService.GetBookingByTokenAsync(cancellationToken);

        if (booking == null)
        {
            return NotFound(new { error = "Booking not found" });
        }

        var business = await _context.BusinessProfiles.FindAsync(booking.BusinessProfileId);

        return Ok(new
        {
            booking = new
            {
                id = booking.Id,
                serviceName = booking.Service?.Name,
                startTime = booking.StartTime,
                endTime = booking.EndTime,
                status = booking.Status.ToString(),
                notes = booking.Notes,
                client = booking.Client
            },
            business = business != null ? new
            {
                name = business.BusinessName,
                phone = business.Phone,
                address = business.Address
            } : null
        });
    }

    // POST /api/book/manage/{cancellationToken}/cancel
    // Cancel a booking
    [HttpPost("manage/{cancellationToken:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid cancellationToken)
    {
        try
        {
            var booking = await _bookingService.CancelBookingByTokenAsync(cancellationToken);

            return Ok(new
            {
                message = "Booking cancelled successfully",
                booking = new
                {
                    id = booking.Id,
                    status = booking.Status.ToString()
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to cancel booking", details = ex.Message });
        }
    }
}

public class CreateBookingRequest
{
    public Guid ServiceId { get; set; }
    public DateTime StartTime { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(255)]
    public string ClientName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string ClientEmail { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? ClientPhone { get; set; }
    
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
