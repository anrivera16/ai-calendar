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

public class BookingServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IGoogleCalendarService> _mockCalendarService;
    private readonly Mock<IAvailabilityService> _mockAvailabilityService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<BookingService>> _mockLogger;
    private readonly BookingService _service;
    private readonly User _testUser;
    private readonly BusinessProfile _businessProfile;
    private readonly Service _testService;
    private readonly Client _testClient;

    public BookingServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _mockCalendarService = new Mock<IGoogleCalendarService>();
        _mockAvailabilityService = new Mock<IAvailabilityService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<BookingService>>();

        _mockConfiguration.Setup(c => c["Frontend:Url"]).Returns("http://localhost:4200");

        _service = new BookingService(
            _context,
            _mockCalendarService.Object,
            _mockAvailabilityService.Object,
            _mockEmailService.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);

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

        _testClient = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(_testClient);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
    }

    [Fact]
    public async Task CreateBookingAsync_CreatesBooking_WithNewClient()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "New Client",
            ClientEmail = "newclient@example.com",
            ClientPhone = "555-1234",
            Notes = "Test booking"
        };

        var result = await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        result.Should().NotBeNull();
        result.Booking.Client.Should().NotBeNull();
        result.Booking.Client!.Name.Should().Be("New Client");
        result.Booking.Client.Email.Should().Be("newclient@example.com");
        result.Booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task CreateBookingAsync_ReusesExistingClient_ByEmail()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Updated Name",
            ClientEmail = _testClient.Email
        };

        var result = await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        result.Booking.ClientId.Should().Be(_testClient.Id);
        result.Booking.Client.Should().NotBeNull();
        result.Booking.Client!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task CreateBookingAsync_CreatesGoogleCalendarEvent_WhenOAuthExists()
    {
        var oauthToken = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_token",
            RefreshToken = "encrypted_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.OAuthTokens.Add(oauthToken);
        await _context.SaveChangesAsync();

        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        var calendarEvent = new CalendarEventDto
        {
            Id = "google-event-123",
            Title = "Test Service - Client",
            Start = createDto.StartTime,
            End = createDto.StartTime.AddMinutes(60)
        };

        _mockCalendarService
            .Setup(s => s.CreateEventAsync(_testUser.Id, It.IsAny<CreateEventDto>()))
            .ReturnsAsync(calendarEvent);

        var result = await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        result.Booking.GoogleCalendarEventId.Should().Be("google-event-123");
        _mockCalendarService.Verify(s => s.CreateEventAsync(_testUser.Id, It.IsAny<CreateEventDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_SkipsGoogleEvent_WhenNoOAuth()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        var result = await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        result.Booking.GoogleCalendarEventId.Should().BeNull();
        _mockCalendarService.Verify(s => s.CreateEventAsync(It.IsAny<Guid>(), It.IsAny<CreateEventDto>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_SendsConfirmationEmail()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        _mockEmailService.Verify(
            s => s.SendBookingConfirmationAsync(
                "client@example.com",
                "Client",
                _businessProfile.BusinessName,
                _testService.Name,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_SendsBusinessNotificationEmail()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com",
            ClientPhone = "555-1234"
        };

        await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        _mockEmailService.Verify(
            s => s.SendNewBookingNotificationAsync(
                _testUser.Email,
                _businessProfile.BusinessName,
                "Client",
                "client@example.com",
                "555-1234",
                _testService.Name,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_GeneratesUniqueCancellationToken()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        var result1 = await _service.CreateBookingAsync(_businessProfile.Id, createDto);
        createDto.StartTime = createDto.StartTime.AddDays(1);
        createDto.ClientEmail = "client2@example.com";
        var result2 = await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        result1.Booking.CancellationToken.Should().NotBe(result2.Booking.CancellationToken);
        result1.Booking.CancellationToken.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateBookingAsync_SetsStatusToConfirmed()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        var result = await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        result.Booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenConflictingBookingExists()
    {
        var startTime = DateTime.UtcNow.AddDays(1);
        var existingBooking = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = startTime,
            EndTime = startTime.AddMinutes(60),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(existingBooking);
        await _context.SaveChangesAsync();

        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = startTime,
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        var act = async () => await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no longer available*");
    }

    [Fact]
    public async Task CreateBookingAsync_Throws_WhenServiceNotFound()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        var act = async () => await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Service not found*");
    }

    [Fact]
    public async Task CancelBookingAsync_SetsStatusToCancelled()
    {
        var booking = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _service.CancelBookingAsync(_businessProfile.Id, booking.Id);

        result.Status.Should().Be(BookingStatus.Cancelled);
    }

    [Fact]
    public async Task CancelBookingAsync_Throws_WhenBookingNotFound()
    {
        var act = async () => await _service.CancelBookingAsync(_businessProfile.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Booking not found*");
    }

    [Fact]
    public async Task CancelBookingByTokenAsync_CancelsCorrectBooking()
    {
        var booking = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed,
            CancellationToken = Guid.NewGuid()
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _service.CancelBookingByTokenAsync(booking.CancellationToken);

        result.Status.Should().Be(BookingStatus.Cancelled);
        result.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task CancelBookingByTokenAsync_Throws_WhenTokenNotFound()
    {
        var act = async () => await _service.CancelBookingByTokenAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Booking not found*");
    }

    [Fact]
    public async Task CancelBookingAsync_DeletesGoogleCalendarEvent_WhenExists()
    {
        var oauthToken = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_token",
            RefreshToken = "encrypted_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.OAuthTokens.Add(oauthToken);

        var booking = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed,
            GoogleCalendarEventId = "google-event-123"
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        await _service.CancelBookingAsync(_businessProfile.Id, booking.Id);

        _mockCalendarService.Verify(
            s => s.DeleteEventAsync(_testUser.Id, "google-event-123"),
            Times.Once);
    }

    [Fact]
    public async Task GetBookingsAsync_FiltersBy_DateRange()
    {
        var startDate = DateTime.UtcNow.Date;
        var booking1 = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = startDate.AddDays(1),
            EndTime = startDate.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed
        };
        var booking2 = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = startDate.AddDays(5),
            EndTime = startDate.AddDays(5).AddHours(1),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.AddRange(booking1, booking2);
        await _context.SaveChangesAsync();

        var result = await _service.GetBookingsAsync(
            _businessProfile.Id,
            startDate,
            startDate.AddDays(3));

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(booking1.Id);
    }

    [Fact]
    public async Task GetBookingsAsync_ReturnsAll_WhenNoFilters()
    {
        var booking1 = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed
        };
        var booking2 = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = DateTime.UtcNow.AddDays(5),
            EndTime = DateTime.UtcNow.AddDays(5).AddHours(1),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.AddRange(booking1, booking2);
        await _context.SaveChangesAsync();

        var result = await _service.GetBookingsAsync(_businessProfile.Id);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsBooking_WithServiceAndClient()
    {
        var booking = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _service.GetBookingByIdAsync(_businessProfile.Id, booking.Id);

        result.Should().NotBeNull();
        result!.Service.Should().NotBeNull();
        result.Service!.Name.Should().Be(_testService.Name);
        result.Client.Should().NotBeNull();
        result.Client!.Email.Should().Be(_testClient.Email);
    }

    [Fact]
    public async Task GetBookingByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetBookingByIdAsync(_businessProfile.Id, Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBookingByTokenAsync_ReturnsBooking_WithDetails()
    {
        var booking = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed,
            CancellationToken = Guid.NewGuid()
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var result = await _service.GetBookingByTokenAsync(booking.CancellationToken);

        result.Should().NotBeNull();
        result!.Id.Should().Be(booking.Id);
        result.Client.Should().NotBeNull();
        result.Client!.Email.Should().Be(_testClient.Email);
    }

    [Fact]
    public async Task CreateBookingAsync_ReturnsManagementUrl()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        var result = await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        result.ManagementUrl.Should().Contain("http://localhost:4200/book/manage/");
        result.ManagementUrl.Should().Contain(result.Booking.CancellationToken.ToString());
    }

    [Fact]
    public async Task CreateBookingAsync_ReturnsBusinessInfo()
    {
        var createDto = new CreateBookingDto
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            ClientName = "Client",
            ClientEmail = "client@example.com"
        };

        var result = await _service.CreateBookingAsync(_businessProfile.Id, createDto);

        result.Business.Should().NotBeNull();
        result.Business.BusinessName.Should().Be(_businessProfile.BusinessName);
    }

    [Fact]
    public async Task CancelBookingAsync_SendsCancellationEmail()
    {
        var booking = new Booking
        {
            BusinessProfileId = _businessProfile.Id,
            ServiceId = _testService.Id,
            ClientId = _testClient.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed
        };
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        await _service.CancelBookingAsync(_businessProfile.Id, booking.Id);

        _mockEmailService.Verify(
            s => s.SendBookingCancellationAsync(
                _testClient.Email,
                _testClient.Name,
                _businessProfile.BusinessName,
                _testService.Name,
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>()),
            Times.Once);
    }
}
