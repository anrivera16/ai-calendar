using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Models.DTOs;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGoogleCalendarService _calendarService;

    public CalendarController(AppDbContext context, IGoogleCalendarService calendarService)
    {
        _context = context;
        _calendarService = calendarService;
    }

    // Helper method to get the authenticated user
    private async Task<User?> GetAuthenticatedUserAsync()
    {
        // Get the first user with OAuth tokens
        var user = await _context.Users
            .Include(u => u.OAuthTokens)
            .FirstOrDefaultAsync(u => u.OAuthTokens.Any());
        
        if (user == null)
        {
            // Try to get any user
            user = await _context.Users.FirstOrDefaultAsync();
        }
        
        return user;
    }

    // GET api/calendar/events?start=2024-01-01T00:00:00Z&end=2024-12-31T23:59:59Z
    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? start, 
        [FromQuery] string? end)
    {
        try
        {
            var user = await GetAuthenticatedUserAsync();
            if (user == null)
            {
                return StatusCode(500, new { error = "No user found. Please authenticate with Google first." });
            }

            DateTime startDate;
            DateTime endDate;

            // Parse dates or use defaults
            if (string.IsNullOrEmpty(start))
            {
                startDate = DateTime.Now.AddMonths(-6);
            }
            else
            {
                startDate = DateTime.Parse(start);
            }

            if (string.IsNullOrEmpty(end))
            {
                endDate = DateTime.Now.AddMonths(6);
            }
            else
            {
                endDate = DateTime.Parse(end);
            }

            var events = await _calendarService.GetEventsAsync(user.Id, startDate, endDate);
            return Ok(events);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // POST api/calendar/events
    [HttpPost("events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventDto eventDto)
    {
        try
        {
            var user = await GetAuthenticatedUserAsync();
            if (user == null)
            {
                return StatusCode(500, new { error = "No user found. Please authenticate with Google first." });
            }

            var createdEvent = await _calendarService.CreateEventAsync(user.Id, eventDto);
            return Ok(createdEvent);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // PUT api/calendar/events/{eventId}
    [HttpPut("events/{eventId}")]
    public async Task<IActionResult> UpdateEvent(
        string eventId, 
        [FromBody] UpdateEventDto eventDto)
    {
        try
        {
            var user = await GetAuthenticatedUserAsync();
            if (user == null)
            {
                return StatusCode(500, new { error = "No user found." });
            }

            var updatedEvent = await _calendarService.UpdateEventAsync(user.Id, eventId, eventDto);
            return Ok(updatedEvent);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // DELETE api/calendar/events/{eventId}
    [HttpDelete("events/{eventId}")]
    public async Task<IActionResult> DeleteEvent(string eventId)
    {
        try
        {
            var user = await GetAuthenticatedUserAsync();
            if (user == null)
            {
                return StatusCode(500, new { error = "No user found." });
            }

            await _calendarService.DeleteEventAsync(user.Id, eventId);
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
