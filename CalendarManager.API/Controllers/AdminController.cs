using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // Helper to get current user from JWT claims
    private async Task<User> GetCurrentUserAsync()
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid or missing user ID in token");
        }
        
        var user = await _context.Users
            .Include(u => u.BusinessProfile)
                .ThenInclude(bp => bp!.Services)
            .Include(u => u.BusinessProfile)
                .ThenInclude(bp => bp!.AvailabilityRules)
            .Include(u => u.UserPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found. Please complete OAuth login first.");
        }

        return user;
    }

    // Helper to generate URL slug from business name
    private string GenerateSlug(string businessName)
    {
        var slug = Regex.Replace(businessName.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = slug.Trim('-');
        return slug;
    }

    // Helper to ensure unique slug
    private async Task<string> EnsureUniqueSlug(string baseSlug, Guid? excludeProfileId = null)
    {
        var slug = baseSlug;
        var counter = 0;
        
        while (true)
        {
            var exists = await _context.BusinessProfiles
                .AnyAsync(bp => bp.Slug == slug && (!excludeProfileId.HasValue || bp.Id != excludeProfileId.Value));
            
            if (!exists) break;
            
            counter++;
            slug = $"{baseSlug}-{counter}";
        }
        
        return slug;
    }

    // ==================== Business Profile ====================

    // GET /api/admin/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await GetCurrentUserAsync();
        
        if (user.BusinessProfile == null)
        {
            return Ok(new { hasProfile = false });
        }

        var profile = user.BusinessProfile;
        return Ok(new
        {
            hasProfile = true,
            id = profile.Id,
            businessName = profile.BusinessName,
            slug = profile.Slug,
            description = profile.Description,
            logoUrl = profile.LogoUrl,
            phone = profile.Phone,
            website = profile.Website,
            address = profile.Address,
            isActive = profile.IsActive,
            createdAt = profile.CreatedAt,
            updatedAt = profile.UpdatedAt,
            bookingUrl = $"/book/{profile.Slug}"
        });
    }

    // POST /api/admin/profile
    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateBusinessProfileDto dto)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile != null)
        {
            return BadRequest(new { error = "Profile already exists. Use PUT to update." });
        }

        var baseSlug = GenerateSlug(dto.BusinessName);
        var slug = await EnsureUniqueSlug(baseSlug);

        var profile = new BusinessProfile
        {
            UserId = user.Id,
            BusinessName = dto.BusinessName,
            Slug = slug,
            Description = dto.Description,
            LogoUrl = dto.LogoUrl,
            Phone = dto.Phone,
            Website = dto.Website,
            Address = dto.Address,
            IsActive = true
        };

        _context.BusinessProfiles.Add(profile);
        await _context.SaveChangesAsync();

        // Auto-seed AvailabilityRules from UserPreferences (Task 3.2)
        await SeedAvailabilityRulesFromPreferences(profile, user.UserPreferences);

        return Ok(new
        {
            id = profile.Id,
            businessName = profile.BusinessName,
            slug = profile.Slug,
            description = profile.Description,
            logoUrl = profile.LogoUrl,
            phone = profile.Phone,
            website = profile.Website,
            address = profile.Address,
            isActive = profile.IsActive,
            bookingUrl = $"/book/{profile.Slug}"
        });
    }

    // PUT /api/admin/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateBusinessProfileDto dto)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Profile not found. Create one first." });
        }

        var profile = user.BusinessProfile;

        // Update fields if provided
        if (!string.IsNullOrEmpty(dto.BusinessName) && dto.BusinessName != profile.BusinessName)
        {
            var baseSlug = GenerateSlug(dto.BusinessName);
            profile.Slug = await EnsureUniqueSlug(baseSlug, profile.Id);
            profile.BusinessName = dto.BusinessName;
        }

        if (dto.Description != null) profile.Description = dto.Description;
        if (dto.LogoUrl != null) profile.LogoUrl = dto.LogoUrl;
        if (dto.Phone != null) profile.Phone = dto.Phone;
        if (dto.Website != null) profile.Website = dto.Website;
        if (dto.Address != null) profile.Address = dto.Address;
        if (dto.IsActive.HasValue) profile.IsActive = dto.IsActive.Value;

        profile.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            id = profile.Id,
            businessName = profile.BusinessName,
            slug = profile.Slug,
            description = profile.Description,
            logoUrl = profile.LogoUrl,
            phone = profile.Phone,
            website = profile.Website,
            address = profile.Address,
            isActive = profile.IsActive,
            bookingUrl = $"/book/{profile.Slug}"
        });
    }

    // Auto-seed AvailabilityRules from UserPreferences
    private async Task SeedAvailabilityRulesFromPreferences(BusinessProfile profile, UserPreferences? preferences)
    {
        if (preferences == null)
        {
            // Default: Monday-Friday, 9am-5pm
            var defaultDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            foreach (var day in defaultDays)
            {
                _context.AvailabilityRules.Add(new AvailabilityRule
                {
                    BusinessProfileId = profile.Id,
                    RuleType = RuleType.Weekly,
                    DayOfWeek = day,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    IsAvailable = true
                });
            }
        }
        else
        {
            // Parse working days from JSON
            var workingDays = System.Text.Json.JsonSerializer.Deserialize<List<string>>(preferences.WorkingDays) 
                ?? new List<string>();

            foreach (var dayStr in workingDays)
            {
                if (Enum.TryParse<DayOfWeek>(dayStr, out var day))
                {
                    _context.AvailabilityRules.Add(new AvailabilityRule
                    {
                        BusinessProfileId = profile.Id,
                        RuleType = RuleType.Weekly,
                        DayOfWeek = day,
                        StartTime = preferences.WorkingHoursStart.ToTimeSpan(),
                        EndTime = preferences.WorkingHoursEnd.ToTimeSpan(),
                        IsAvailable = true
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    // ==================== Services ====================

    // GET /api/admin/services
    [HttpGet("services")]
    public async Task<IActionResult> GetServices()
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var services = await _context.Services
            .Where(s => s.BusinessProfileId == user.BusinessProfile.Id)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return Ok(services.Select(s => new ServiceDto
        {
            Id = s.Id,
            BusinessProfileId = s.BusinessProfileId,
            Name = s.Name,
            Description = s.Description,
            DurationMinutes = s.DurationMinutes,
            Price = s.Price,
            Color = s.Color,
            IsActive = s.IsActive,
            SortOrder = s.SortOrder,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        }));
    }

    // POST /api/admin/services
    [HttpPost("services")]
    public async Task<IActionResult> CreateService([FromBody] CreateServiceDto dto)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var service = new Service
        {
            BusinessProfileId = user.BusinessProfile.Id,
            Name = dto.Name,
            Description = dto.Description,
            DurationMinutes = dto.DurationMinutes,
            Price = dto.Price,
            Color = dto.Color,
            IsActive = dto.IsActive,
            SortOrder = dto.SortOrder
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return Ok(new ServiceDto
        {
            Id = service.Id,
            BusinessProfileId = service.BusinessProfileId,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            Color = service.Color,
            IsActive = service.IsActive,
            SortOrder = service.SortOrder,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt
        });
    }

    // PUT /api/admin/services/{id}
    [HttpPut("services/{id:guid}")]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceDto dto)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessProfileId == user.BusinessProfile.Id);

        if (service == null)
        {
            return NotFound(new { error = "Service not found" });
        }

        if (dto.Name != null) service.Name = dto.Name;
        if (dto.Description != null) service.Description = dto.Description;
        if (dto.DurationMinutes.HasValue) service.DurationMinutes = dto.DurationMinutes.Value;
        if (dto.Price.HasValue) service.Price = dto.Price.Value;
        if (dto.Color != null) service.Color = dto.Color;
        if (dto.IsActive.HasValue) service.IsActive = dto.IsActive.Value;
        if (dto.SortOrder.HasValue) service.SortOrder = dto.SortOrder.Value;

        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new ServiceDto
        {
            Id = service.Id,
            BusinessProfileId = service.BusinessProfileId,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            Color = service.Color,
            IsActive = service.IsActive,
            SortOrder = service.SortOrder,
            CreatedAt = service.CreatedAt,
            UpdatedAt = service.UpdatedAt
        });
    }

    // DELETE /api/admin/services/{id}
    [HttpDelete("services/{id:guid}")]
    public async Task<IActionResult> DeleteService(Guid id)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == id && s.BusinessProfileId == user.BusinessProfile.Id);

        if (service == null)
        {
            return NotFound(new { error = "Service not found" });
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Service deleted successfully" });
    }

    // ==================== Availability ====================

    // GET /api/admin/availability
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability()
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var rules = await _context.AvailabilityRules
            .Where(r => r.BusinessProfileId == user.BusinessProfile.Id)
            .OrderBy(r => r.DayOfWeek)
            .ThenBy(r => r.StartTime)
            .ToListAsync();

        return Ok(rules.Select(r => new AvailabilityRuleDto
        {
            Id = r.Id,
            BusinessProfileId = r.BusinessProfileId,
            RuleType = r.RuleType,
            DayOfWeek = r.DayOfWeek,
            StartTime = r.StartTime,
            EndTime = r.EndTime,
            SpecificDate = r.SpecificDate,
            IsAvailable = r.IsAvailable,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }));
    }

    // POST /api/admin/availability/weekly
    [HttpPost("availability/weekly")]
    public async Task<IActionResult> CreateWeeklyAvailability([FromBody] CreateWeeklyAvailabilityDto dto)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        // Remove existing rule for this day if it exists
        var existingRule = await _context.AvailabilityRules
            .FirstOrDefaultAsync(r => 
                r.BusinessProfileId == user.BusinessProfile.Id &&
                r.RuleType == RuleType.Weekly &&
                r.DayOfWeek == dto.DayOfWeek);

        if (existingRule != null)
        {
            existingRule.StartTime = dto.StartTime;
            existingRule.EndTime = dto.EndTime;
            existingRule.IsAvailable = dto.IsAvailable;
            existingRule.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var rule = new AvailabilityRule
            {
                BusinessProfileId = user.BusinessProfile.Id,
                RuleType = RuleType.Weekly,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsAvailable = dto.IsAvailable
            };
            _context.AvailabilityRules.Add(rule);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Weekly availability updated" });
    }

    // POST /api/admin/availability/override
    [HttpPost("availability/override")]
    public async Task<IActionResult> CreateDateOverride([FromBody] CreateDateOverrideDto dto)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        // Remove existing override for this date if it exists
        var existingOverride = await _context.AvailabilityRules
            .FirstOrDefaultAsync(r => 
                r.BusinessProfileId == user.BusinessProfile.Id &&
                r.RuleType == RuleType.DateOverride &&
                r.SpecificDate == dto.SpecificDate.Date);

        if (existingOverride != null)
        {
            existingOverride.StartTime = dto.StartTime;
            existingOverride.EndTime = dto.EndTime;
            existingOverride.IsAvailable = dto.IsAvailable;
            existingOverride.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var rule = new AvailabilityRule
            {
                BusinessProfileId = user.BusinessProfile.Id,
                RuleType = RuleType.DateOverride,
                SpecificDate = dto.SpecificDate.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                IsAvailable = dto.IsAvailable
            };
            _context.AvailabilityRules.Add(rule);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Date override created" });
    }

    // POST /api/admin/availability/break
    [HttpPost("availability/break")]
    public async Task<IActionResult> CreateBreak([FromBody] CreateBreakDto dto)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var rule = new AvailabilityRule
        {
            BusinessProfileId = user.BusinessProfile.Id,
            RuleType = RuleType.Break,
            SpecificDate = dto.SpecificDate.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            IsAvailable = false
        };

        _context.AvailabilityRules.Add(rule);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Break created" });
    }

    // DELETE /api/admin/availability/{id}
    [HttpDelete("availability/{id:guid}")]
    public async Task<IActionResult> DeleteAvailabilityRule(Guid id)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var rule = await _context.AvailabilityRules
            .FirstOrDefaultAsync(r => r.Id == id && r.BusinessProfileId == user.BusinessProfile.Id);

        if (rule == null)
        {
            return NotFound(new { error = "Availability rule not found" });
        }

        _context.AvailabilityRules.Remove(rule);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Availability rule deleted" });
    }

    // ==================== Bookings ====================

    // GET /api/admin/bookings
    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings([FromQuery] string? status = null, [FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var query = _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Where(b => b.BusinessProfileId == user.BusinessProfile.Id);

        // Filter by status
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<BookingStatus>(status, true, out var bookingStatus))
        {
            query = query.Where(b => b.Status == bookingStatus);
        }

        // Filter by date range
        if (fromDate.HasValue)
        {
            query = query.Where(b => b.StartTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.StartTime <= toDate.Value);
        }

        var bookings = await query
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        return Ok(bookings.Select(b => new
        {
            id = b.Id,
            serviceId = b.ServiceId,
            serviceName = b.Service?.Name,
            serviceColor = b.Service?.Color,
            clientName = b.Client?.Name,
            clientEmail = b.Client?.Email,
            clientPhone = b.Client?.Phone,
            startTime = b.StartTime,
            endTime = b.EndTime,
            status = b.Status.ToString(),
            notes = b.Notes,
            createdAt = b.CreatedAt
        }));
    }

    // GET /api/admin/bookings/today
    [HttpGet("bookings/today")]
    public async Task<IActionResult> GetTodaysBookings()
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var bookings = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Where(b => b.BusinessProfileId == user.BusinessProfile.Id &&
                        b.StartTime >= today &&
                        b.StartTime < tomorrow &&
                        b.Status == BookingStatus.Confirmed)
            .OrderBy(b => b.StartTime)
            .ToListAsync();

        return Ok(bookings.Select(b => new
        {
            id = b.Id,
            serviceId = b.ServiceId,
            serviceName = b.Service?.Name,
            serviceColor = b.Service?.Color,
            clientName = b.Client?.Name,
            clientEmail = b.Client?.Email,
            clientPhone = b.Client?.Phone,
            startTime = b.StartTime,
            endTime = b.EndTime,
            status = b.Status.ToString(),
            notes = b.Notes
        }));
    }

    // GET /api/admin/bookings/upcoming
    [HttpGet("bookings/upcoming")]
    public async Task<IActionResult> GetUpcomingBookings([FromQuery] int limit = 10)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var now = DateTime.UtcNow;

        var bookings = await _context.Bookings
            .Include(b => b.Service)
            .Include(b => b.Client)
            .Where(b => b.BusinessProfileId == user.BusinessProfile.Id &&
                        b.StartTime >= now &&
                        b.Status == BookingStatus.Confirmed)
            .OrderBy(b => b.StartTime)
            .Take(limit)
            .ToListAsync();

        return Ok(bookings.Select(b => new
        {
            id = b.Id,
            serviceId = b.ServiceId,
            serviceName = b.Service?.Name,
            serviceColor = b.Service?.Color,
            clientName = b.Client?.Name,
            clientEmail = b.Client?.Email,
            startTime = b.StartTime,
            endTime = b.EndTime,
            status = b.Status.ToString()
        }));
    }

    // PUT /api/admin/bookings/{id}/cancel
    [HttpPut("bookings/{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id && b.BusinessProfileId == user.BusinessProfile.Id);

        if (booking == null)
        {
            return NotFound(new { error = "Booking not found" });
        }

        if (booking.Status == BookingStatus.Cancelled)
        {
            return BadRequest(new { error = "Booking is already cancelled" });
        }

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Booking cancelled successfully", status = "Cancelled" });
    }

    // PUT /api/admin/bookings/{id}/complete
    [HttpPut("bookings/{id:guid}/complete")]
    public async Task<IActionResult> CompleteBooking(Guid id)
    {
        var user = await GetCurrentUserAsync();

        if (user.BusinessProfile == null)
        {
            return NotFound(new { error = "Business profile not found" });
        }

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id && b.BusinessProfileId == user.BusinessProfile.Id);

        if (booking == null)
        {
            return NotFound(new { error = "Booking not found" });
        }

        booking.Status = BookingStatus.Completed;
        booking.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Booking marked as completed", status = "Completed" });
    }
}
