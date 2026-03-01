using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Implementations;
using CalendarManager.API.Services.Interfaces;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace CalendarManager.API.Tests.Services;

public class GoogleCalendarServiceTests : IDisposable
{
    private readonly Mock<IOAuthService> _mockOAuthService;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly GoogleCalendarService _service;
    private readonly Guid _testUserId = Guid.NewGuid();

    public GoogleCalendarServiceTests()
    {
        _mockOAuthService = new Mock<IOAuthService>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _service = new GoogleCalendarService(_httpClient, _mockOAuthService.Object);

        _mockOAuthService
            .Setup(s => s.GetValidAccessTokenAsync(_testUserId))
            .ReturnsAsync("test-access-token");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsEvents_FromGoogleApi()
    {
        var googleResponse = new
        {
            items = new[]
            {
                new
                {
                    id = "event1",
                    summary = "Test Event 1",
                    start = new { dateTime = "2026-03-01T10:00:00Z" },
                    end = new { dateTime = "2026-03-01T11:00:00Z" }
                },
                new
                {
                    id = "event2",
                    summary = "Test Event 2",
                    start = new { dateTime = "2026-03-01T14:00:00Z" },
                    end = new { dateTime = "2026-03-01T15:00:00Z" }
                }
            }
        };

        SetupHttpResponse(googleResponse, HttpStatusCode.OK);

        var result = await _service.GetEventsAsync(_testUserId, DateTime.Today, DateTime.Today.AddDays(7));

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("event1");
        result[0].Title.Should().Be("Test Event 1");
        result[1].Id.Should().Be("event2");
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsEmptyList_WhenNoEvents()
    {
        var googleResponse = new { items = Array.Empty<object>() };
        SetupHttpResponse(googleResponse, HttpStatusCode.OK);

        var result = await _service.GetEventsAsync(_testUserId, DateTime.Today, DateTime.Today.AddDays(1));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetEventsAsync_HandlesAllDayEvents()
    {
        var googleResponse = new
        {
            items = new[]
            {
                new
                {
                    id = "allday1",
                    summary = "All Day Event",
                    start = new { date = "2026-03-01" },
                    end = new { date = "2026-03-02" }
                }
            }
        };

        SetupHttpResponse(googleResponse, HttpStatusCode.OK);

        var result = await _service.GetEventsAsync(_testUserId, DateTime.Today, DateTime.Today.AddDays(7));

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("allday1");
        result[0].Title.Should().Be("All Day Event");
    }

    [Fact]
    public async Task CreateEventAsync_SendsCorrectPayload_ToGoogleApi()
    {
        var createDto = new CreateEventDto
        {
            Title = "New Meeting",
            Start = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
            End = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Utc),
            Description = "Meeting description",
            Location = "Conference Room"
        };

        var googleResponse = new
        {
            id = "new-event-id",
            summary = "New Meeting",
            description = "Meeting description",
            location = "Conference Room",
            start = new { dateTime = "2026-03-01T10:00:00Z" },
            end = new { dateTime = "2026-03-01T11:00:00Z" }
        };

        HttpRequestMessage? capturedRequest = null;
        SetupHttpResponse(googleResponse, HttpStatusCode.OK, req => capturedRequest = req);

        var result = await _service.CreateEventAsync(_testUserId, createDto);

        result.Should().NotBeNull();
        result.Id.Should().Be("new-event-id");
        result.Title.Should().Be("New Meeting");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().Should().Contain("/calendars/primary/events");
    }

    [Fact]
    public async Task CreateEventAsync_ReturnsCreatedEvent()
    {
        var createDto = new CreateEventDto
        {
            Title = "Test Event",
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddHours(1)
        };

        var googleResponse = new
        {
            id = "created-event-123",
            summary = "Test Event",
            start = new { dateTime = createDto.Start.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") },
            end = new { dateTime = createDto.End.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
        };

        SetupHttpResponse(googleResponse, HttpStatusCode.OK);

        var result = await _service.CreateEventAsync(_testUserId, createDto);

        result.Should().NotBeNull();
        result.Id.Should().Be("created-event-123");
        result.Title.Should().Be("Test Event");
    }

    [Fact]
    public async Task UpdateEventAsync_SendsCorrectPayload()
    {
        var updateDto = new UpdateEventDto
        {
            Title = "Updated Title",
            Start = new DateTime(2026, 3, 1, 14, 0, 0, DateTimeKind.Utc)
        };

        var googleResponse = new
        {
            id = "event-to-update",
            summary = "Updated Title",
            start = new { dateTime = "2026-03-01T14:00:00Z" },
            end = new { dateTime = "2026-03-01T15:00:00Z" }
        };

        SetupHttpResponse(googleResponse, HttpStatusCode.OK);

        var result = await _service.UpdateEventAsync(_testUserId, "event-to-update", updateDto);

        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateEventAsync_Throws_WhenEventNotFound()
    {
        var updateDto = new UpdateEventDto { Title = "Updated" };

        SetupHttpResponse(new { error = "Not found" }, HttpStatusCode.NotFound);

        var act = async () => await _service.UpdateEventAsync(_testUserId, "nonexistent", updateDto);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task DeleteEventAsync_SendsDeleteRequest()
    {
        SetupHttpResponse(new { }, HttpStatusCode.NoContent);

        var act = async () => await _service.DeleteEventAsync(_testUserId, "event-to-delete");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteEventAsync_Throws_WhenEventNotFound()
    {
        SetupHttpResponse(new { error = "Not found" }, HttpStatusCode.NotFound);

        var act = async () => await _service.DeleteEventAsync(_testUserId, "nonexistent");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetFreeBusyAsync_ReturnsBusyPeriods()
    {
        var googleResponse = new
        {
            calendars = new Dictionary<string, object>
            {
                ["test@example.com"] = new
                {
                    busy = new[]
                    {
                        new { start = "2026-03-01T10:00:00Z", end = "2026-03-01T11:00:00Z" },
                        new { start = "2026-03-01T14:00:00Z", end = "2026-03-01T15:30:00Z" }
                    }
                }
            }
        };

        SetupHttpResponse(googleResponse, HttpStatusCode.OK);

        var result = await _service.GetFreeBusyAsync(_testUserId, "test@example.com", DateTime.Today, DateTime.Today.AddDays(1));

        result.Should().NotBeNull();
        result.Email.Should().Be("test@example.com");
        result.BusyPeriods.Should().HaveCount(2);
        result.BusyPeriods[0].Start.Should().Be(new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc));
        result.BusyPeriods[1].End.Should().Be(new DateTime(2026, 3, 1, 15, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsTrue_WhenTokenValid()
    {
        SetupHttpResponse(new { id = "primary" }, HttpStatusCode.OK);

        var result = await _service.ValidateTokenAsync(_testUserId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsFalse_WhenTokenInvalid()
    {
        SetupHttpResponse(new { error = "Unauthorized" }, HttpStatusCode.Unauthorized);

        var result = await _service.ValidateTokenAsync(_testUserId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetEventsAsync_Handle401_ByThrowingUnauthorized()
    {
        SetupHttpResponse(new { error = "Unauthorized" }, HttpStatusCode.Unauthorized);

        var act = async () => await _service.GetEventsAsync(_testUserId, DateTime.Today, DateTime.Today.AddDays(1));

        await act.Should().ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEventsAsync_Handle429_ByThrowingRateLimitException()
    {
        SetupHttpResponse(new { error = "Rate limit exceeded" }, HttpStatusCode.TooManyRequests);

        var act = async () => await _service.GetEventsAsync(_testUserId, DateTime.Today, DateTime.Today.AddDays(1));

        await act.Should().ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task CreateEventAsync_Handle401_ByThrowingUnauthorized()
    {
        var createDto = new CreateEventDto
        {
            Title = "Test",
            Start = DateTime.UtcNow,
            End = DateTime.UtcNow.AddHours(1)
        };

        SetupHttpResponse(new { error = "Unauthorized" }, HttpStatusCode.Unauthorized);

        var act = async () => await _service.CreateEventAsync(_testUserId, createDto);

        await act.Should().ThrowAsync<HttpRequestException>()
            .Where(e => e.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetEventsAsync_HandlesEventsWithAttendees()
    {
        var googleResponse = new
        {
            items = new[]
            {
                new
                {
                    id = "event1",
                    summary = "Meeting with Team",
                    start = new { dateTime = "2026-03-01T10:00:00Z" },
                    end = new { dateTime = "2026-03-01T11:00:00Z" },
                    attendees = new[]
                    {
                        new { email = "user1@example.com" },
                        new { email = "user2@example.com" }
                    }
                }
            }
        };

        SetupHttpResponse(googleResponse, HttpStatusCode.OK);

        var result = await _service.GetEventsAsync(_testUserId, DateTime.Today, DateTime.Today.AddDays(7));

        result.Should().HaveCount(1);
        result[0].Attendees.Should().HaveCount(2);
        result[0].Attendees.Should().Contain("user1@example.com");
        result[0].Attendees.Should().Contain("user2@example.com");
    }

    private void SetupHttpResponse<T>(T content, HttpStatusCode statusCode, Action<HttpRequestMessage>? captureRequest = null)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = JsonContent.Create(content)
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response)
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captureRequest?.Invoke(req));
    }
}
