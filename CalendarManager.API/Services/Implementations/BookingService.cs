using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CalendarManager.API.Services.Implementations;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;
    private readonly IGoogleCalendarService _calendarService;
    private readonly IAvailabilityService _availabilityService;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingService> _logger;
    private readonly string _frontendUrl;
    
    public BookingService(
        AppDbContext context,
        IGoogleCalendarService calendarService,
        IAvailabilityService availabilityService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<BookingService> logger)
    {
        _context = context;
        _calendarService = calendarService;
        _availabilityService = availabilityService;
        _emailService = emailService;
        _logger = logger;
        _frontendUrl = configuration["Frontend:Url"] ?? "http://localhost:4200";
    }
    
    public async Task<BookingConfirmationDto> CreateBookingAsync(Guid businessProfileId, CreateBookingDto createBooking)
    {
        // Validate service exists and belongs to business
        var service = await _context.Services.FindAsync(createBooking.ServiceId);
        if (service == null || service.BusinessProfileId != businessProfileId)
        {
            throw new InvalidOperationException("Service not found");
        }
        
        // Validate slot availability - re-check at database level to prevent race conditions
        var endTime = createBooking.StartTime.AddMinutes(service.DurationMinutes);
        
        // Check for existing confirmed bookings that overlap with requested slot
        var conflictingBooking = await _context.Bookings
            .Where(b => b.BusinessProfileId == businessProfileId
                     && b.Status == BookingStatus.Confirmed
                     && b.StartTime < endTime
                     && b.EndTime > createBooking.StartTime)
            .FirstOrDefaultAsync();
        
        if (conflictingBooking != null)
        {
            throw new InvalidOperationException("Selected time slot is no longer available");
        }
        
        // Get business profile for Google Calendar (need to fetch it anyway)
        var businessProfile = await _context.BusinessProfiles
            .Include(bp => bp.User)
            .FirstOrDefaultAsync(bp => bp.Id == businessProfileId);
        
        if (businessProfile == null)
        {
            throw new InvalidOperationException("Business not found");
        }
        
        // Attempt to create booking - may still fail if slot is taken (defense in depth)
        try
        {
            return await CreateBookingInternalAsync(businessProfileId, createBooking, service, businessProfile);
        }
        catch (DbUpdateException ex) when (IsConflictException(ex))
        {
            throw new InvalidOperationException("Selected time slot is no longer available");
        }
    }
    
    private async Task<BookingConfirmationDto> CreateBookingInternalAsync(
        Guid businessProfileId, 
        CreateBookingDto createBooking,
        Service service,
        BusinessProfile businessProfile)
    {
        // Find or create client
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.Email == createBooking.ClientEmail);
        
        if (client == null)
        {
            client = new Client
            {
                Name = createBooking.ClientName,
                Email = createBooking.ClientEmail,
                Phone = createBooking.ClientPhone
            };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Update client info if provided
            if (!string.IsNullOrEmpty(createBooking.ClientName))
            {
                client.Name = createBooking.ClientName;
            }
            if (!string.IsNullOrEmpty(createBooking.ClientPhone))
            {
                client.Phone = createBooking.ClientPhone;
            }
        }
        
        var endTime = createBooking.StartTime.AddMinutes(service.DurationMinutes);
        
        // Create booking
        var booking = new Booking
        {
            BusinessProfileId = businessProfileId,
            ServiceId = createBooking.ServiceId,
            ClientId = client.Id,
            StartTime = createBooking.StartTime,
            EndTime = endTime,
            Status = BookingStatus.Confirmed,
            Notes = createBooking.Notes,
            CancellationToken = Guid.NewGuid()
        };
        
        // Create Google Calendar event if user has OAuth
        string? googleEventId = null;
        if (businessProfile.UserId != Guid.Empty)
        {
            try
            {
                var oauthToken = await _context.OAuthTokens
                    .Where(t => t.UserId == businessProfile.UserId && t.ExpiresAt > DateTime.UtcNow)
                    .FirstOrDefaultAsync();
                
                if (oauthToken != null)
                {
                    var createEventDto = new CreateEventDto
                    {
                        Title = $"{service.Name} - {client.Name}",
                        Description = $"Booking for {service.Name}\nClient: {client.Name}\nEmail: {client.Email}\n{createBooking.Notes ?? ""}",
                        Start = createBooking.StartTime,
                        End = endTime,
                        Attendees = new List<string> { client.Email }
                    };
                    
                    var googleEvent = await _calendarService.CreateEventAsync(
                        businessProfile.UserId,
                        createEventDto);
                    
                    googleEventId = googleEvent.Id;
                    booking.GoogleCalendarEventId = googleEventId;
                }
            }
            catch (Exception ex)
            {
                // If Google Calendar fails, continue without it but log the error
                _logger.LogWarning(ex, "Failed to create Google Calendar event for booking. Booking {BookingId} created without calendar sync.", booking.Id);
            }
        }
        
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        
        // Send confirmation email to client
        var managementUrl = $"{_frontendUrl}/book/manage/{booking.CancellationToken}";
        try
        {
            await _emailService.SendBookingConfirmationAsync(
                client.Email,
                client.Name,
                businessProfile.BusinessName,
                service.Name,
                booking.StartTime,
                booking.EndTime,
                managementUrl,
                booking.Notes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send booking confirmation email for booking {BookingId}", booking.Id);
        }
        
        // Send notification to business owner
        try
        {
            await _emailService.SendNewBookingNotificationAsync(
                businessProfile.User.Email,
                businessProfile.BusinessName,
                client.Name,
                client.Email,
                client.Phone,
                service.Name,
                booking.StartTime,
                booking.EndTime,
                booking.Notes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send new booking notification email for booking {BookingId}", booking.Id);
        }
        
        // Build confirmation
        var bookingDto = await GetBookingByIdAsync(businessProfileId, booking.Id);
        
        return new BookingConfirmationDto
        {
            Booking = bookingDto!,
            ManagementUrl = $"{_frontendUrl}/book/manage/{booking.CancellationToken}",
            Business = MapBusinessToDto(businessProfile)
        };
    }
    
    private static bool IsConflictException(DbUpdateException ex)
    {
        // Check for unique constraint violation (race condition on slot)
        // PostgreSQL unique violation error code is 23505
        return ex.InnerException?.Message?.Contains("23505") == true ||
               ex.InnerException?.Message?.Contains("unique constraint") == true ||
               ex.InnerException?.Message?.Contains("duplicate key") == true;
    }
    
    public async Task<BookingDto> CancelBookingAsync(Guid businessProfileId, Guid bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Include(b => b.BusinessProfile)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.BusinessProfileId == businessProfileId);
        
        if (booking == null)
        {
            throw new InvalidOperationException("Booking not found");
        }
        
        return await CancelBookingInternalAsync(booking);
    }
    
    public async Task<BookingDto> CancelBookingByTokenAsync(Guid cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Include(b => b.BusinessProfile)
            .FirstOrDefaultAsync(b => b.CancellationToken == cancellationToken);
        
        if (booking == null)
        {
            throw new InvalidOperationException("Booking not found");
        }
        
        return await CancelBookingInternalAsync(booking);
    }
    
    private async Task<BookingDto> CancelBookingInternalAsync(Booking booking)
    {
        // Delete Google Calendar event if exists
        if (!string.IsNullOrEmpty(booking.GoogleCalendarEventId))
        {
            var businessProfile = await _context.BusinessProfiles
                .Include(bp => bp.User)
                .FirstOrDefaultAsync(bp => bp.Id == booking.BusinessProfileId);
            
            if (businessProfile?.UserId != null && businessProfile.UserId != Guid.Empty)
            {
                try
                {
                    await _calendarService.DeleteEventAsync(
                        businessProfile.UserId,
                        booking.GoogleCalendarEventId);
                }
                catch
                {
                    // Ignore errors deleting from Google Calendar
                }
            }
        }
        
        booking.Status = BookingStatus.Cancelled;
        await _context.SaveChangesAsync();
        
        // Send cancellation email to client
        try
        {
            await _emailService.SendBookingCancellationAsync(
                booking.Client.Email,
                booking.Client.Name,
                booking.BusinessProfile.BusinessName,
                booking.Service.Name,
                booking.StartTime,
                booking.EndTime);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send cancellation email for booking {BookingId}", booking.Id);
        }
        
        return MapToDto(booking);
    }
    
    public async Task<BookingDto?> GetBookingByTokenAsync(Guid cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.CancellationToken == cancellationToken);
        
        return booking == null ? null : MapToDto(booking);
    }
    
    public async Task<List<BookingDto>> GetBookingsAsync(
        Guid businessProfileId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Where(b => b.BusinessProfileId == businessProfileId);
        
        if (startDate.HasValue)
        {
            query = query.Where(b => b.StartTime >= startDate.Value);
        }
        
        if (endDate.HasValue)
        {
            query = query.Where(b => b.StartTime < endDate.Value);
        }
        
        var bookings = await query
            .OrderBy(b => b.StartTime)
            .ToListAsync();
        
        return bookings.Select(MapToDto).ToList();
    }
    
    public async Task<BookingDto?> GetBookingByIdAsync(Guid businessProfileId, Guid bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.BusinessProfileId == businessProfileId);
        
        return booking == null ? null : MapToDto(booking);
    }
    
    private static BookingDto MapToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            BusinessProfileId = booking.BusinessProfileId,
            ServiceId = booking.ServiceId,
            ClientId = booking.ClientId,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            Notes = booking.Notes,
            GoogleCalendarEventId = booking.GoogleCalendarEventId,
            CancellationToken = booking.CancellationToken,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            Service = booking.Service == null ? null : new ServiceDto
            {
                Id = booking.Service.Id,
                BusinessProfileId = booking.Service.BusinessProfileId,
                Name = booking.Service.Name,
                Description = booking.Service.Description,
                DurationMinutes = booking.Service.DurationMinutes,
                Price = booking.Service.Price,
                Color = booking.Service.Color,
                IsActive = booking.Service.IsActive,
                SortOrder = booking.Service.SortOrder,
                CreatedAt = booking.Service.CreatedAt,
                UpdatedAt = booking.Service.UpdatedAt
            },
            Client = booking.Client == null ? null : new ClientDto
            {
                Id = booking.Client.Id,
                Name = booking.Client.Name,
                Email = booking.Client.Email,
                Phone = booking.Client.Phone,
                CreatedAt = booking.Client.CreatedAt
            }
        };
    }
    
    private static BusinessProfileDto MapBusinessToDto(BusinessProfile businessProfile)
    {
        return new BusinessProfileDto
        {
            Id = businessProfile.Id,
            UserId = businessProfile.UserId,
            BusinessName = businessProfile.BusinessName,
            Slug = businessProfile.Slug,
            Description = businessProfile.Description,
            LogoUrl = businessProfile.LogoUrl,
            Phone = businessProfile.Phone,
            Website = businessProfile.Website,
            Address = businessProfile.Address,
            IsActive = businessProfile.IsActive,
            CreatedAt = businessProfile.CreatedAt,
            UpdatedAt = businessProfile.UpdatedAt
        };
    }
}
