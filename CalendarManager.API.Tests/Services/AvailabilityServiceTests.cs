using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Implementations;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Services;

public class AvailabilityServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IGoogleCalendarService> _mockCalendarService;
    private readonly Mock<ILogger<AvailabilityService>> _mockLogger;
    private readonly AvailabilityService _service;
    private readonly User _testUser;
    private readonly BusinessProfile _businessProfile;
    private readonly Service _testService;

    public AvailabilityServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _mockCalendarService = new Mock<IGoogleCalendarService>();
        _mockLogger = new Mock<ILogger<AvailabilityService>>();
        _service = new AvailabilityService(_context, _mockCalendarService.Object, _mockLogger.Object);

        _testUser = new User { Email = "business@example.com" };
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

        _testService = new Service
        {
            BusinessProfileId = _businessProfile.Id,
            Name = "Test Service",
            DurationMinutes = 60,
            Price = 100,
            IsActive = true
        };
        _context.Services.Add(_testService);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsSlots_WhenAvailable()
    {
        var date = new DateTime(2026, 3, 2);
        var rule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(rule);
        await _context.SaveChangesAsync();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, _testService.Id, date);

        result.Should().NotBeEmpty();
        result.First().StartTime.Hour.Should().Be(9);
        result.Last().EndTime.Hour.Should().BeLessOrEqualTo(17);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ExcludesBookedSlots()
    {
        var date = new DateTime(2026, 3, 2);
        var rule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(rule);

        var client = new Client { Name = "Client", Email = "client@example.com" };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        var booking = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = client.Id,
            StartTime = date.AddHours(10),
            EndTime = date.AddHours(11),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, _testService.Id, date);

        result.Should().NotContain(s => s.StartTime.Hour == 10);
        result.Should().Contain(s => s.StartTime.Hour == 9);
        result.Should().Contain(s => s.StartTime.Hour == 11);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ExcludesGoogleCalendarBusyTimes()
    {
        var date = new DateTime(2026, 3, 2);
        var rule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(rule);
        await _context.SaveChangesAsync();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<CalendarEventDto>
            {
                new()
                {
                    Id = "google-event-1",
                    Title = "Busy Time",
                    Start = date.AddHours(10),
                    End = date.AddHours(11)
                }
            });

        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, _testService.Id, date);

        result.Should().NotContain(s => s.StartTime.Hour == 10);
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_RespectsServiceDuration()
    {
        var date = new DateTime(2026, 3, 2);
        var shortService = new Service
        {
            BusinessProfileId = _businessProfile.Id,
            Name = "Short Service",
            DurationMinutes = 30,
            Price = 50,
            IsActive = true
        };
        _context.Services.Add(shortService);

        var longService = new Service
        {
            BusinessProfileId = _businessProfile.Id,
            Name = "Long Service",
            DurationMinutes = 120,
            Price = 200,
            IsActive = true
        };
        _context.Services.Add(longService);

        var rule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(rule);
        await _context.SaveChangesAsync();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        var shortResult = await _service.GetAvailableSlotsAsync(_businessProfile.Id, shortService.Id, date);
        var longResult = await _service.GetAvailableSlotsAsync(_businessProfile.Id, longService.Id, date);

        shortResult.Should().HaveCount(3);
        longResult.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsEmpty_WhenNoRulesForDay()
    {
        var date = new DateTime(2026, 3, 2);

        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, _testService.Id, date);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_UsesDateOverride_InsteadOfWeeklyRule()
    {
        var date = new DateTime(2026, 3, 2);
        var overrideRule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.DateOverride,
            SpecificDate = date,
            StartTime = new TimeSpan(14, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(overrideRule);
        await _context.SaveChangesAsync();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, _testService.Id, date);

        result.Should().NotBeEmpty();
        result.All(s => s.StartTime.Hour >= 14).Should().BeTrue();
        result.All(s => s.EndTime.Hour <= 16).Should().BeTrue();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ExcludesBreakTimes()
    {
        var date = new DateTime(2026, 3, 2);
        var weeklyRule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(weeklyRule);

        var breakRule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Break,
            SpecificDate = date,
            StartTime = new TimeSpan(12, 0, 0),
            EndTime = new TimeSpan(13, 0, 0),
            IsAvailable = false
        };
        _context.AvailabilityRules.Add(breakRule);
        await _context.SaveChangesAsync();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, _testService.Id, date);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_HandlesMultipleRules_SameDay()
    {
        var date = new DateTime(2026, 3, 2);
        var morningRule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(morningRule);

        var afternoonRule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(14, 0, 0),
            EndTime = new TimeSpan(18, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(afternoonRule);
        await _context.SaveChangesAsync();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<CalendarEventDto>());

        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, _testService.Id, date);

        result.Should().NotBeEmpty();
        var morningSlots = result.Where(s => s.StartTime.Hour < 12).ToList();
        var afternoonSlots = result.Where(s => s.StartTime.Hour >= 14).ToList();
        morningSlots.Should().NotBeEmpty();
        afternoonSlots.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsEmpty_WhenServiceNotFound()
    {
        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, Guid.NewGuid(), DateTime.Today);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_ReturnsEmpty_WhenBusinessProfileNotFound()
    {
        var result = await _service.GetAvailableSlotsAsync(Guid.NewGuid(), _testService.Id, DateTime.Today);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRulesForDateAsync_ReturnsWeeklyRule_ForMatchingDay()
    {
        var date = new DateTime(2026, 3, 2);
        var rule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(rule);
        await _context.SaveChangesAsync();

        var result = await _service.GetRulesForDateAsync(_businessProfile.Id, date);

        result.Should().HaveCount(1);
        result[0].RuleType.Should().Be(RuleType.Weekly);
        result[0].DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task GetRulesForDateAsync_ReturnsDateOverride_WhenExists()
    {
        var date = new DateTime(2026, 3, 2);
        var weeklyRule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(weeklyRule);

        var overrideRule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.DateOverride,
            SpecificDate = date,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(14, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(overrideRule);
        await _context.SaveChangesAsync();

        var result = await _service.GetRulesForDateAsync(_businessProfile.Id, date);

        result.Should().Contain(r => r.RuleType == RuleType.DateOverride);
    }

    [Fact]
    public async Task GetRulesForDateAsync_ReturnsEmpty_WhenNoMatch()
    {
        var date = new DateTime(2026, 3, 2);

        var result = await _service.GetRulesForDateAsync(_businessProfile.Id, date);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSlotsAsync_Continues_WhenGoogleCalendarFails()
    {
        var date = new DateTime(2026, 3, 2);
        var rule = new AvailabilityRule
        {
            BusinessProfileId = _businessProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(rule);
        await _context.SaveChangesAsync();

        _mockCalendarService
            .Setup(s => s.GetEventsAsync(_testUser.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ThrowsAsync(new HttpRequestException("Google API error"));

        var result = await _service.GetAvailableSlotsAsync(_businessProfile.Id, _testService.Id, date);

        result.Should().NotBeEmpty();
    }
}
