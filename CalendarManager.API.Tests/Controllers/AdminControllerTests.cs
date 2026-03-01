using CalendarManager.API.Controllers;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AdminController _controller;
    private readonly User _testUser;
    private readonly BusinessProfile _testProfile;

    public AdminControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["DemoMode"]).Returns("true");
        
        _controller = new AdminController(_context, _mockConfiguration.Object);

        _testUser = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        _context.Users.Add(_testUser);

        _testProfile = new BusinessProfile
        {
            UserId = _testUser.Id,
            BusinessName = "Test Business",
            Slug = "test-business",
            IsActive = true
        };
        _context.BusinessProfiles.Add(_testProfile);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
    }

    [Fact]
    public async Task GetProfile_Returns200_WithProfile_WhenExists()
    {
        var result = await _controller.GetProfile();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var hasProfile = (bool?)value?.GetType().GetProperty("hasProfile")?.GetValue(value);
        hasProfile.Should().BeTrue();
    }

    [Fact]
    public async Task GetProfile_Returns200_WithHasProfileFalse_WhenNotExists()
    {
        _context.BusinessProfiles.Remove(_testProfile);
        await _context.SaveChangesAsync();

        var result = await _controller.GetProfile();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        var hasProfile = (bool?)value?.GetType().GetProperty("hasProfile")?.GetValue(value);
        hasProfile.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProfile_Returns200_WithCreatedProfile()
    {
        _context.BusinessProfiles.Remove(_testProfile);
        await _context.SaveChangesAsync();

        var dto = new CreateBusinessProfileDto
        {
            BusinessName = "New Business",
            Description = "A new business",
            Phone = "555-1234"
        };

        var result = await _controller.CreateProfile(dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var businessName = value?.GetType().GetProperty("businessName")?.GetValue(value)?.ToString();
        businessName.Should().Be("New Business");
    }

    [Fact]
    public async Task CreateProfile_GeneratesSlug_FromBusinessName()
    {
        _context.BusinessProfiles.Remove(_testProfile);
        await _context.SaveChangesAsync();

        var dto = new CreateBusinessProfileDto
        {
            BusinessName = "My Awesome Business!"
        };

        var result = await _controller.CreateProfile(dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        var slug = value?.GetType().GetProperty("slug")?.GetValue(value)?.ToString();
        slug.Should().Be("my-awesome-business");
    }

    [Fact]
    public async Task CreateProfile_ReturnsBadRequest_WhenProfileAlreadyExists()
    {
        var dto = new CreateBusinessProfileDto
        {
            BusinessName = "Another Business"
        };

        var result = await _controller.CreateProfile(dto);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateProfile_Returns200_WithUpdatedProfile()
    {
        var dto = new UpdateBusinessProfileDto
        {
            BusinessName = "Updated Business",
            Description = "Updated description"
        };

        var result = await _controller.UpdateProfile(dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        var businessName = value?.GetType().GetProperty("businessName")?.GetValue(value)?.ToString();
        businessName.Should().Be("Updated Business");
    }

    [Fact]
    public async Task UpdateProfile_ReturnsNotFound_WhenNotExists()
    {
        _context.BusinessProfiles.Remove(_testProfile);
        await _context.SaveChangesAsync();

        var dto = new UpdateBusinessProfileDto { BusinessName = "Updated" };

        var result = await _controller.UpdateProfile(dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetServices_Returns200_WithServiceList()
    {
        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var result = await _controller.GetServices();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateService_Returns200_WithCreatedService()
    {
        var dto = new CreateServiceDto
        {
            Name = "New Service",
            DurationMinutes = 60,
            Price = 100.00m
        };

        var result = await _controller.CreateService(dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var serviceDto = okResult.Value.Should().BeOfType<ServiceDto>().Subject;
        serviceDto.Name.Should().Be("New Service");
        serviceDto.DurationMinutes.Should().Be(60);
    }

    [Fact]
    public async Task CreateService_ReturnsNotFound_WhenNoBusinessProfile()
    {
        _context.BusinessProfiles.Remove(_testProfile);
        await _context.SaveChangesAsync();

        var dto = new CreateServiceDto
        {
            Name = "New Service",
            DurationMinutes = 60,
            Price = 100.00m
        };

        var result = await _controller.CreateService(dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateService_Returns200_WithUpdatedService()
    {
        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var dto = new UpdateServiceDto
        {
            Name = "Updated Service",
            DurationMinutes = 45
        };

        var result = await _controller.UpdateService(service.Id, dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var serviceDto = okResult.Value.Should().BeOfType<ServiceDto>().Subject;
        serviceDto.Name.Should().Be("Updated Service");
        serviceDto.DurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task UpdateService_ReturnsNotFound_WhenNotFound()
    {
        var dto = new UpdateServiceDto { Name = "Updated" };

        var result = await _controller.UpdateService(Guid.NewGuid(), dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteService_Returns200_OnSuccess()
    {
        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Service to Delete",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteService(service.Id);

        result.Should().BeOfType<OkObjectResult>();
        var deletedService = _context.Services.FirstOrDefault(s => s.Id == service.Id);
        deletedService.Should().BeNull();
    }

    [Fact]
    public async Task DeleteService_ReturnsNotFound_WhenNotFound()
    {
        var result = await _controller.DeleteService(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAvailability_Returns200_WithRules()
    {
        var rule = new AvailabilityRule
        {
            BusinessProfileId = _testProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(rule);
        await _context.SaveChangesAsync();

        var result = await _controller.GetAvailability();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateWeeklyAvailability_Returns200_WithRule()
    {
        var dto = new CreateWeeklyAvailabilityDto
        {
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(18, 0, 0),
            IsAvailable = true
        };

        var result = await _controller.CreateWeeklyAvailability(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateWeeklyAvailability_UpdatesExistingRule_WhenExists()
    {
        var existingRule = new AvailabilityRule
        {
            BusinessProfileId = _testProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(existingRule);
        await _context.SaveChangesAsync();

        var dto = new CreateWeeklyAvailabilityDto
        {
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            IsAvailable = true
        };

        await _controller.CreateWeeklyAvailability(dto);

        var rule = _context.AvailabilityRules.First(r => r.DayOfWeek == DayOfWeek.Monday);
        rule.StartTime.Should().Be(new TimeSpan(8, 0, 0));
        rule.EndTime.Should().Be(new TimeSpan(16, 0, 0));
    }

    [Fact]
    public async Task CreateDateOverride_Returns200_WithRule()
    {
        var dto = new CreateDateOverrideDto
        {
            SpecificDate = DateTime.UtcNow.AddDays(7),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            IsAvailable = true
        };

        var result = await _controller.CreateDateOverride(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateBreak_Returns200_WithRule()
    {
        var dto = new CreateBreakDto
        {
            SpecificDate = DateTime.UtcNow.AddDays(1),
            StartTime = new TimeSpan(12, 0, 0),
            EndTime = new TimeSpan(13, 0, 0)
        };

        var result = await _controller.CreateBreak(dto);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeleteAvailability_Returns200_OnSuccess()
    {
        var rule = new AvailabilityRule
        {
            BusinessProfileId = _testProfile.Id,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsAvailable = true
        };
        _context.AvailabilityRules.Add(rule);
        await _context.SaveChangesAsync();

        var result = await _controller.DeleteAvailabilityRule(rule.Id);

        result.Should().BeOfType<OkObjectResult>();
        var deletedRule = _context.AvailabilityRules.FirstOrDefault(r => r.Id == rule.Id);
        deletedRule.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAvailability_ReturnsNotFound_WhenNotFound()
    {
        var result = await _controller.DeleteAvailabilityRule(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBookings_Returns200_WithList()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);

        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var booking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _controller.GetBookings();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookings_AppliesFilters_WhenProvided()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);

        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var confirmedBooking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = BookingStatus.Confirmed
        };
        var cancelledBooking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddMinutes(30),
            Status = BookingStatus.Cancelled
        };
        _context.Bookings.AddRange(confirmedBooking, cancelledBooking);
        await _context.SaveChangesAsync();

        var result = await _controller.GetBookings(status: "Confirmed");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookings_AppliesDateFilters_WhenProvided()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);

        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var booking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddMinutes(30),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _controller.GetBookings(
            fromDate: DateTime.UtcNow.AddDays(3),
            toDate: DateTime.UtcNow.AddDays(7));

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTodaysBookings_ReturnsOnlyTodayBookings()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);

        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var todayBooking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(30),
            Status = BookingStatus.Confirmed
        };
        var tomorrowBooking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.AddRange(todayBooking, tomorrowBooking);
        await _context.SaveChangesAsync();

        var result = await _controller.GetTodaysBookings();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUpcomingBookings_RespectsLimit()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);

        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        for (int i = 0; i < 15; i++)
        {
            var booking = new Booking
            {
                BusinessProfileId = _testProfile.Id,
                ServiceId = service.Id,
                ClientId = client.Id,
                StartTime = DateTime.UtcNow.AddDays(i + 1),
                EndTime = DateTime.UtcNow.AddDays(i + 1).AddMinutes(30),
                Status = BookingStatus.Confirmed
            };
            _context.Bookings.Add(booking);
        }
        await _context.SaveChangesAsync();

        var result = await _controller.GetUpcomingBookings(limit: 5);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelBooking_Returns200_WithUpdatedStatus()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);

        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var booking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _controller.CancelBooking(booking.Id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var cancelledBooking = _context.Bookings.First(b => b.Id == booking.Id);
        cancelledBooking.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelBooking_ReturnsNotFound_WhenNotFound()
    {
        var result = await _controller.CancelBooking(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelBooking_ReturnsBadRequest_WhenAlreadyCancelled()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);

        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var booking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = BookingStatus.Cancelled
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _controller.CancelBooking(booking.Id);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CompleteBooking_Returns200_WithUpdatedStatus()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);

        var service = new Service
        {
            BusinessProfileId = _testProfile.Id,
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 50.00m
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var booking = new Booking
        {
            BusinessProfileId = _testProfile.Id,
            ServiceId = service.Id,
            ClientId = client.Id,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(-1).AddMinutes(30),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _controller.CompleteBooking(booking.Id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var completedBooking = _context.Bookings.First(b => b.Id == booking.Id);
        completedBooking.Status.Should().Be(BookingStatus.Completed);
    }

    [Fact]
    public async Task CompleteBooking_ReturnsNotFound_WhenNotFound()
    {
        var result = await _controller.CompleteBooking(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetServices_ReturnsNotFound_WhenNoBusinessProfile()
    {
        _context.BusinessProfiles.Remove(_testProfile);
        await _context.SaveChangesAsync();

        var result = await _controller.GetServices();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAvailability_ReturnsNotFound_WhenNoBusinessProfile()
    {
        _context.BusinessProfiles.Remove(_testProfile);
        await _context.SaveChangesAsync();

        var result = await _controller.GetAvailability();

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBookings_ReturnsNotFound_WhenNoBusinessProfile()
    {
        _context.BusinessProfiles.Remove(_testProfile);
        await _context.SaveChangesAsync();

        var result = await _controller.GetBookings();

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
