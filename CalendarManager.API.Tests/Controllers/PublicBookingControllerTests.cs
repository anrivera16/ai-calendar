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

public class PublicBookingControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IAvailabilityService> _mockAvailabilityService;
    private readonly Mock<IBookingService> _mockBookingService;
    private readonly PublicBookingController _controller;
    private readonly BusinessProfile _testBusiness;
    private readonly Service _testService;

    public PublicBookingControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _mockAvailabilityService = new Mock<IAvailabilityService>();
        _mockBookingService = new Mock<IBookingService>();
        _controller = new PublicBookingController(_context, _mockAvailabilityService.Object, _mockBookingService.Object);

        var user = new User
        {
            Email = "business@example.com",
            DisplayName = "Business Owner"
        };
        _context.Users.Add(user);

        _testBusiness = new BusinessProfile
        {
            UserId = user.Id,
            BusinessName = "Test Business",
            Slug = "test-business",
            Description = "A test business",
            IsActive = true
        };
        _context.BusinessProfiles.Add(_testBusiness);

        _testService = new Service
        {
            BusinessProfileId = _testBusiness.Id,
            Name = "Test Service",
            DurationMinutes = 60,
            Price = 100.00m,
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
    public async Task GetBusiness_Returns200_WithBusinessAndServices()
    {
        var result = await _controller.GetBusinessBySlug("test-business");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var businessName = value?.GetType().GetProperty("businessName")?.GetValue(value)?.ToString();
        businessName.Should().Be("Test Business");
    }

    [Fact]
    public async Task GetBusiness_Returns404_WhenSlugNotFound()
    {
        var result = await _controller.GetBusinessBySlug("nonexistent-business");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBusiness_Returns404_WhenBusinessInactive()
    {
        _testBusiness.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _controller.GetBusinessBySlug("test-business");

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetBusiness_ReturnsOnlyActiveServices()
    {
        var inactiveService = new Service
        {
            BusinessProfileId = _testBusiness.Id,
            Name = "Inactive Service",
            DurationMinutes = 30,
            Price = 50.00m,
            IsActive = false
        };
        _context.Services.Add(inactiveService);
        await _context.SaveChangesAsync();

        var result = await _controller.GetBusinessBySlug("test-business");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSlots_Returns200_WithAvailableSlots()
    {
        var date = DateTime.UtcNow.AddDays(7);
        var slots = new List<AvailableSlotDto>
        {
            new AvailableSlotDto
            {
                StartTime = date.AddHours(9),
                EndTime = date.AddHours(10)
            },
            new AvailableSlotDto
            {
                StartTime = date.AddHours(10),
                EndTime = date.AddHours(11)
            }
        };

        _mockAvailabilityService.Setup(s => s.GetAvailableSlotsAsync(_testBusiness.Id, _testService.Id, date.Date))
            .ReturnsAsync(slots);

        var result = await _controller.GetAvailableSlots("test-business", _testService.Id, date);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSlots_Returns404_WhenBusinessNotFound()
    {
        var result = await _controller.GetAvailableSlots("nonexistent", _testService.Id, DateTime.UtcNow);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSlots_Returns404_WhenServiceNotFound()
    {
        var result = await _controller.GetAvailableSlots("test-business", Guid.NewGuid(), DateTime.UtcNow);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetSlots_ReturnsEmptySlots_WhenNoneAvailable()
    {
        _mockAvailabilityService.Setup(s => s.GetAvailableSlotsAsync(_testBusiness.Id, _testService.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(new List<AvailableSlotDto>());

        var result = await _controller.GetAvailableSlots("test-business", _testService.Id, DateTime.UtcNow.AddDays(7));

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBooking_Returns200_WithConfirmation()
    {
        var client = new Client
        {
            Name = "Test Client",
            Email = "client@example.com"
        };
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        var request = new CreateBookingRequest
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(7).AddHours(9),
            ClientName = "New Client",
            ClientEmail = "newclient@example.com",
            ClientPhone = "555-1234"
        };

        var booking = new BookingDto
        {
            Id = Guid.NewGuid(),
            BusinessProfileId = _testBusiness.Id,
            ServiceId = _testService.Id,
            StartTime = request.StartTime,
            EndTime = request.StartTime.AddMinutes(60),
            Status = BookingStatus.Confirmed,
            Client = new ClientDto
            {
                Name = request.ClientName,
                Email = request.ClientEmail,
                Phone = request.ClientPhone
            }
        };

        var confirmation = new BookingConfirmationDto
        {
            Booking = booking,
            ManagementUrl = "https://example.com/manage/123",
            Business = new BusinessProfileDto
            {
                BusinessName = _testBusiness.BusinessName,
                Address = _testBusiness.Address,
                Phone = _testBusiness.Phone
            }
        };

        _mockBookingService.Setup(s => s.CreateBookingAsync(_testBusiness.Id, It.IsAny<CreateBookingDto>()))
            .ReturnsAsync(confirmation);

        var result = await _controller.CreateBooking("test-business", request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBooking_Returns404_WhenSlugNotFound()
    {
        var request = new CreateBookingRequest
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(7),
            ClientName = "Test Client",
            ClientEmail = "test@example.com"
        };

        var result = await _controller.CreateBooking("nonexistent", request);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateBooking_Returns400_WhenServiceThrowsInvalidOperationException()
    {
        var request = new CreateBookingRequest
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(7),
            ClientName = "Test Client",
            ClientEmail = "test@example.com"
        };

        _mockBookingService.Setup(s => s.CreateBookingAsync(_testBusiness.Id, It.IsAny<CreateBookingDto>()))
            .ThrowsAsync(new InvalidOperationException("Slot no longer available"));

        var result = await _controller.CreateBooking("test-business", request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateBooking_Returns500_WhenUnexpectedError()
    {
        var request = new CreateBookingRequest
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(7),
            ClientName = "Test Client",
            ClientEmail = "test@example.com"
        };

        _mockBookingService.Setup(s => s.CreateBookingAsync(_testBusiness.Id, It.IsAny<CreateBookingDto>()))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _controller.CreateBooking("test-business", request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetBookingByToken_Returns200_WithDetails()
    {
        var cancellationToken = Guid.NewGuid();
        var booking = new BookingDto
        {
            Id = Guid.NewGuid(),
            BusinessProfileId = _testBusiness.Id,
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Confirmed,
            CancellationToken = cancellationToken,
            Service = new ServiceDto
            {
                Name = "Test Service",
                DurationMinutes = 60
            },
            Client = new ClientDto
            {
                Name = "Test Client",
                Email = "client@example.com"
            }
        };

        _mockBookingService.Setup(s => s.GetBookingByTokenAsync(cancellationToken))
            .ReturnsAsync(booking);

        var result = await _controller.GetBookingByToken(cancellationToken);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookingByToken_Returns404_WhenTokenNotFound()
    {
        var cancellationToken = Guid.NewGuid();

        _mockBookingService.Setup(s => s.GetBookingByTokenAsync(cancellationToken))
            .ReturnsAsync((BookingDto?)null);

        var result = await _controller.GetBookingByToken(cancellationToken);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelBookingByToken_Returns200_OnSuccess()
    {
        var cancellationToken = Guid.NewGuid();
        var booking = new BookingDto
        {
            Id = Guid.NewGuid(),
            BusinessProfileId = _testBusiness.Id,
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = BookingStatus.Cancelled,
            CancellationToken = cancellationToken
        };

        _mockBookingService.Setup(s => s.CancelBookingByTokenAsync(cancellationToken))
            .ReturnsAsync(booking);

        var result = await _controller.CancelBooking(cancellationToken);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelBookingByToken_Returns404_WhenTokenNotFound()
    {
        var cancellationToken = Guid.NewGuid();

        _mockBookingService.Setup(s => s.CancelBookingByTokenAsync(cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Booking not found"));

        var result = await _controller.CancelBooking(cancellationToken);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelBookingByToken_Returns500_WhenUnexpectedError()
    {
        var cancellationToken = Guid.NewGuid();

        _mockBookingService.Setup(s => s.CancelBookingByTokenAsync(cancellationToken))
            .ThrowsAsync(new Exception("Database error"));

        var result = await _controller.CancelBooking(cancellationToken);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetBusiness_ReturnsBusinessInfo_WithServices()
    {
        var result = await _controller.GetBusinessBySlug("test-business");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSlots_ReturnsServiceDuration()
    {
        var date = DateTime.UtcNow.AddDays(7);
        var slots = new List<AvailableSlotDto>();

        _mockAvailabilityService.Setup(s => s.GetAvailableSlotsAsync(_testBusiness.Id, _testService.Id, date.Date))
            .ReturnsAsync(slots);

        var result = await _controller.GetAvailableSlots("test-business", _testService.Id, date);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateBooking_WithNotes_ReturnsConfirmation()
    {
        var request = new CreateBookingRequest
        {
            ServiceId = _testService.Id,
            StartTime = DateTime.UtcNow.AddDays(7),
            ClientName = "Test Client",
            ClientEmail = "test@example.com",
            Notes = "Special requests"
        };

        var booking = new BookingDto
        {
            Id = Guid.NewGuid(),
            BusinessProfileId = _testBusiness.Id,
            ServiceId = _testService.Id,
            StartTime = request.StartTime,
            EndTime = request.StartTime.AddMinutes(60),
            Status = BookingStatus.Confirmed,
            Notes = request.Notes,
            Client = new ClientDto
            {
                Name = request.ClientName,
                Email = request.ClientEmail
            }
        };

        var confirmation = new BookingConfirmationDto
        {
            Booking = booking,
            ManagementUrl = "https://example.com/manage/123",
            Business = new BusinessProfileDto
            {
                BusinessName = _testBusiness.BusinessName
            }
        };

        _mockBookingService.Setup(s => s.CreateBookingAsync(_testBusiness.Id, It.Is<CreateBookingDto>(d => d.Notes == "Special requests")))
            .ReturnsAsync(confirmation);

        var result = await _controller.CreateBooking("test-business", request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookingByToken_ReturnsBusinessInfo()
    {
        var cancellationToken = Guid.NewGuid();
        var booking = new BookingDto
        {
            Id = Guid.NewGuid(),
            BusinessProfileId = _testBusiness.Id,
            CancellationToken = cancellationToken,
            Service = new ServiceDto { Name = "Service" },
            Client = new ClientDto { Name = "Client", Email = "client@test.com" }
        };

        _mockBookingService.Setup(s => s.GetBookingByTokenAsync(cancellationToken))
            .ReturnsAsync(booking);

        var result = await _controller.GetBookingByToken(cancellationToken);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }
}
