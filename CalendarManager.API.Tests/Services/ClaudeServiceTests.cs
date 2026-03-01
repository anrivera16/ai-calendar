using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic.SDK.Messaging;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Implementations;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Services;

public class ClaudeServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IGoogleCalendarService> _mockCalendarService;
    private readonly Mock<IBookingService> _mockBookingService;
    private readonly Mock<IAvailabilityService> _mockAvailabilityService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ClaudeService>> _mockLogger;
    private readonly User _testUser;
    private readonly BusinessProfile _businessProfile;

    public ClaudeServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _mockCalendarService = new Mock<IGoogleCalendarService>();
        _mockBookingService = new Mock<IBookingService>();
        _mockAvailabilityService = new Mock<IAvailabilityService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ClaudeService>>();

        Environment.SetEnvironmentVariable("CLAUDE_API_KEY", "test-api-key-for-testing");
        _mockConfiguration.Setup(c => c["Claude:FallbackOnly"]).Returns("true");
        _mockConfiguration.Setup(c => c["Claude:ApiKey"]).Returns("test-api-key");

        _testUser = new User { Email = "test@example.com" };
        _context.Users.Add(_testUser);
        _context.SaveChanges();

        _businessProfile = new BusinessProfile
        {
            UserId = _testUser.Id,
            BusinessName = "Test Business",
            Slug = "test-business",
            IsActive = true
        };
        _context.BusinessProfiles.Add(_businessProfile);
        _context.SaveChanges();
    }

    private ClaudeService CreateService()
    {
        return new ClaudeService(
            _mockConfiguration.Object,
            _mockCalendarService.Object,
            _mockBookingService.Object,
            _mockAvailabilityService.Object,
            _mockLogger.Object,
            _context);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CLAUDE_API_KEY", null);
        TestDbContextFactory.Destroy(_context);
    }

    [Fact]
    public async Task ProcessMessageAsync_ReturnsResponse_ForSimpleMessage()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Hello", _testUser.Email);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessMessageAsync_ReturnsError_WhenUserNotFound()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Hello", "nonexistent@example.com");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Authentication required");
    }

    [Fact]
    public async Task ProcessMessageAsync_ReturnsConversationId()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Hello", _testUser.Email);

        result.ConversationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessMessageAsync_UsesProvidedConversationId()
    {
        var service = CreateService();
        var conversationId = "existing-conversation-123";

        var result = await service.ProcessMessageAsync("Hello", _testUser.Email, conversationId);

        result.ConversationId.Should().Be(conversationId);
    }

    [Fact]
    public async Task ProcessMessageAsync_HandlesSchedulingIntent()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Schedule a meeting tomorrow", _testUser.Email);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("schedule", "Should mention scheduling");
    }

    [Fact]
    public async Task ProcessMessageAsync_HandlesViewingIntent()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("What's on my calendar today?", _testUser.Email);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessMessageAsync_HandlesHelpIntent()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Help me", _testUser.Email);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("calendar", "Should mention calendar");
    }

    [Fact]
    public async Task ProcessMessageAsync_HandlesGreeting()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Hello there!", _testUser.Email);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessMessageAsync_FallsBackOnError()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Test message", _testUser.Email);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCalendarEventAsync_DelegatesToGoogleCalendarService()
    {
        var service = CreateService();
        var toolCall = new CreateEventToolCall
        {
            Title = "Test Event",
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Description = "Test description"
        };

        var calendarEvent = new CalendarEventDto
        {
            Id = "event-123",
            Title = "Test Event",
            Start = toolCall.StartTime,
            End = toolCall.EndTime
        };

        _mockCalendarService
            .Setup(s => s.CreateEventAsync(_testUser.Id, It.IsAny<CreateEventDto>()))
            .ReturnsAsync(calendarEvent);

        var result = await service.CreateCalendarEventAsync(toolCall, _testUser.Email);

        result.Should().NotBeNull();
        result.Executed.Should().BeTrue();
        result.Type.Should().Be("create_calendar_event");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCalendarEventAsync_ReturnsError_WhenUserNotFound()
    {
        var service = CreateService();
        var toolCall = new CreateEventToolCall
        {
            Title = "Test Event",
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };

        var result = await service.CreateCalendarEventAsync(toolCall, "nonexistent@example.com");

        result.Executed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }

    [Fact]
    public async Task ListCalendarEventsAsync_DelegatesToGoogleCalendarService()
    {
        var service = CreateService();
        var toolCall = new ListEventsToolCall
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(7),
            MaxResults = 10
        };

        var events = new List<CalendarEventDto>
        {
            new() { Id = "1", Title = "Event 1", Start = DateTime.Today, End = DateTime.Today.AddHours(1) },
            new() { Id = "2", Title = "Event 2", Start = DateTime.Today.AddDays(1), End = DateTime.Today.AddDays(1).AddHours(1) }
        };

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(events);

        var result = await service.ListCalendarEventsAsync(toolCall, _testUser.Email);

        result.Should().NotBeNull();
        result.Executed.Should().BeTrue();
        result.Type.Should().Be("list_calendar_events");
    }

    [Fact]
    public async Task ListCalendarEventsAsync_ReturnsError_WhenUserNotFound()
    {
        var service = CreateService();
        var toolCall = new ListEventsToolCall();

        var result = await service.ListCalendarEventsAsync(toolCall, "nonexistent@example.com");

        result.Executed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }

    [Fact]
    public async Task FindFreeTimeAsync_DelegatesToGoogleCalendarService()
    {
        var service = CreateService();
        var toolCall = new FindFreeTimeToolCall
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            DurationMinutes = 60
        };

        var freeBusy = new FreeBusyDto
        {
            Email = _testUser.Email,
            BusyPeriods = new List<BusyPeriod>
            {
                new() { Start = DateTime.Today.AddHours(9), End = DateTime.Today.AddHours(10) }
            }
        };

        _mockCalendarService
            .Setup(s => s.GetFreeBusyAsync(_testUser.Id, _testUser.Email, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(freeBusy);

        var result = await service.FindFreeTimeAsync(toolCall, _testUser.Email);

        result.Should().NotBeNull();
        result.Executed.Should().BeTrue();
        result.Type.Should().Be("find_free_time");
    }

    [Fact]
    public async Task FindFreeTimeAsync_ReturnsError_WhenUserNotFound()
    {
        var service = CreateService();
        var toolCall = new FindFreeTimeToolCall
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            DurationMinutes = 60
        };

        var result = await service.FindFreeTimeAsync(toolCall, "nonexistent@example.com");

        result.Executed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("User not found");
    }

    [Fact]
    public async Task ProcessMessageAsync_HandlesCancellationIntent()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Cancel my meeting", _testUser.Email);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessMessageAsync_HandlesTimeQuery()
    {
        var service = CreateService();

        var result = await service.ProcessMessageAsync("When am I free?", _testUser.Email);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCalendarEventAsync_HandlesException()
    {
        var service = CreateService();
        var toolCall = new CreateEventToolCall
        {
            Title = "Test Event",
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };

        _mockCalendarService
            .Setup(s => s.CreateEventAsync(_testUser.Id, It.IsAny<CreateEventDto>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        var result = await service.CreateCalendarEventAsync(toolCall, _testUser.Email);

        result.Executed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("API error");
    }

    [Fact]
    public async Task ListCalendarEventsAsync_HandlesException()
    {
        var service = CreateService();
        var toolCall = new ListEventsToolCall();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        var result = await service.ListCalendarEventsAsync(toolCall, _testUser.Email);

        result.Executed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("API error");
    }

    [Fact]
    public async Task FindFreeTimeAsync_HandlesException()
    {
        var service = CreateService();
        var toolCall = new FindFreeTimeToolCall
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            DurationMinutes = 60
        };

        _mockCalendarService
            .Setup(s => s.GetFreeBusyAsync(_testUser.Id, _testUser.Email, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new HttpRequestException("API error"));

        var result = await service.FindFreeTimeAsync(toolCall, _testUser.Email);

        result.Executed.Should().BeFalse();
        result.ErrorMessage.Should().Contain("API error");
    }

    [Fact]
    public async Task ProcessMessageAsync_WithFallbackOnly_UsesPatternMatching()
    {
        _mockConfiguration.Setup(c => c["Claude:FallbackOnly"]).Returns("true");
        var service = CreateService();

        var result = await service.ProcessMessageAsync("Hello", _testUser.Email);

        result.Success.Should().BeTrue();
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_Throws_WhenNoApiKey()
    {
        Environment.SetEnvironmentVariable("CLAUDE_API_KEY", null);
        _mockConfiguration.Setup(c => c["Claude:ApiKey"]).Returns((string?)null);

        var act = () => CreateService();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Claude API key not found*");
    }
}
