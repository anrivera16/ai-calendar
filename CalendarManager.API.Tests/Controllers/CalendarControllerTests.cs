using CalendarManager.API.Controllers;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Controllers;

public class CalendarControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IGoogleCalendarService> _mockCalendarService;
    private readonly CalendarController _controller;
    private readonly User _testUser;

    public CalendarControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _mockCalendarService = new Mock<IGoogleCalendarService>();
        _controller = new CalendarController(_context, _mockCalendarService.Object);

        _testUser = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        _context.Users.Add(_testUser);
        
        var oauthToken = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_access",
            RefreshToken = "encrypted_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.OAuthTokens.Add(oauthToken);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
    }

    [Fact]
    public async Task GetEvents_Returns200_WithEvents()
    {
        var events = new List<CalendarEventDto>
        {
            new CalendarEventDto
            {
                Id = "event1",
                Title = "Test Event",
                Start = DateTime.UtcNow,
                End = DateTime.UtcNow.AddHours(1)
            }
        };

        _mockCalendarService.Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(events);

        var result = await _controller.GetEvents(null, null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(events);
    }

    [Fact]
    public async Task GetEvents_Returns200_WithEmptyList_WhenNoEvents()
    {
        _mockCalendarService.Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        var result = await _controller.GetEvents(null, null);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(new List<CalendarEventDto>());
    }

    [Fact]
    public async Task GetEvents_UsesDefaultDateRange_WhenNotProvided()
    {
        var capturedStart = DateTime.MinValue;
        var capturedEnd = DateTime.MinValue;

        _mockCalendarService.Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<Guid, DateTime, DateTime>((_, start, end) =>
            {
                capturedStart = start;
                capturedEnd = end;
            })
            .ReturnsAsync(new List<CalendarEventDto>());

        await _controller.GetEvents(null, null);

        capturedStart.Should().BeCloseTo(DateTime.Now.AddMonths(-6), TimeSpan.FromMinutes(1));
        capturedEnd.Should().BeCloseTo(DateTime.Now.AddMonths(6), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetEvents_UsesProvidedDateRange()
    {
        var start = "2024-01-01T00:00:00Z";
        var end = "2024-12-31T23:59:59Z";
        var capturedStart = DateTime.MinValue;
        var capturedEnd = DateTime.MinValue;

        _mockCalendarService.Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Callback<Guid, DateTime, DateTime>((_, s, e) =>
            {
                capturedStart = s;
                capturedEnd = e;
            })
            .ReturnsAsync(new List<CalendarEventDto>());

        await _controller.GetEvents(start, end);

        capturedStart.Should().Be(DateTime.Parse(start));
        capturedEnd.Should().Be(DateTime.Parse(end));
    }

    [Fact]
    public async Task CreateEvent_Returns200_WithCreatedEvent()
    {
        var eventDto = new CreateEventDto
        {
            Title = "New Event",
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddHours(1)
        };

        var createdEvent = new CalendarEventDto
        {
            Id = "new-event-id",
            Title = "New Event",
            Start = eventDto.Start,
            End = eventDto.End
        };

        _mockCalendarService.Setup(s => s.CreateEventAsync(_testUser.Id, eventDto))
            .ReturnsAsync(createdEvent);

        var result = await _controller.CreateEvent(eventDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(createdEvent);
    }

    [Fact]
    public async Task CreateEvent_Returns500_WhenServiceThrows()
    {
        var eventDto = new CreateEventDto
        {
            Title = "New Event",
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddHours(1)
        };

        _mockCalendarService.Setup(s => s.CreateEventAsync(_testUser.Id, eventDto))
            .ThrowsAsync(new Exception("API error"));

        var result = await _controller.CreateEvent(eventDto);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdateEvent_Returns200_WithUpdatedEvent()
    {
        var eventId = "event-to-update";
        var updateDto = new UpdateEventDto
        {
            Title = "Updated Title"
        };

        var updatedEvent = new CalendarEventDto
        {
            Id = eventId,
            Title = "Updated Title",
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddHours(1)
        };

        _mockCalendarService.Setup(s => s.UpdateEventAsync(_testUser.Id, eventId, updateDto))
            .ReturnsAsync(updatedEvent);

        var result = await _controller.UpdateEvent(eventId, updateDto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(updatedEvent);
    }

    [Fact]
    public async Task UpdateEvent_Returns500_WhenEventNotFound()
    {
        var eventId = "nonexistent";
        var updateDto = new UpdateEventDto { Title = "Updated" };

        _mockCalendarService.Setup(s => s.UpdateEventAsync(_testUser.Id, eventId, updateDto))
            .ThrowsAsync(new Exception("Event not found"));

        var result = await _controller.UpdateEvent(eventId, updateDto);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task DeleteEvent_Returns200_OnSuccess()
    {
        var eventId = "event-to-delete";

        _mockCalendarService.Setup(s => s.DeleteEventAsync(_testUser.Id, eventId))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteEvent(eventId);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task DeleteEvent_Returns500_WhenEventNotFound()
    {
        var eventId = "nonexistent";

        _mockCalendarService.Setup(s => s.DeleteEventAsync(_testUser.Id, eventId))
            .ThrowsAsync(new Exception("Event not found"));

        var result = await _controller.DeleteEvent(eventId);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetEvents_Returns500_WhenNoAuthenticatedUser()
    {
        _context.OAuthTokens.RemoveRange(_context.OAuthTokens);
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();

        var result = await _controller.GetEvents(null, null);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateEvent_Returns500_WhenNoAuthenticatedUser()
    {
        _context.OAuthTokens.RemoveRange(_context.OAuthTokens);
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();

        var eventDto = new CreateEventDto
        {
            Title = "New Event",
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddHours(1)
        };

        var result = await _controller.CreateEvent(eventDto);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdateEvent_Returns500_WhenNoAuthenticatedUser()
    {
        _context.OAuthTokens.RemoveRange(_context.OAuthTokens);
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();

        var updateDto = new UpdateEventDto { Title = "Updated" };

        var result = await _controller.UpdateEvent("event-id", updateDto);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task DeleteEvent_Returns500_WhenNoAuthenticatedUser()
    {
        _context.OAuthTokens.RemoveRange(_context.OAuthTokens);
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();

        var result = await _controller.DeleteEvent("event-id");

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }
}
