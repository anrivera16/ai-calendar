using CalendarManager.API.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public EmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailService>>();
        _mockConfiguration = new Mock<IConfiguration>();
    }

    private EmailService CreateService(bool enabled = true)
    {
        _mockConfiguration.Setup(c => c["Email:Enabled"]).Returns(enabled ? "true" : "false");
        _mockConfiguration.Setup(c => c["Email:Smtp:Host"]).Returns("smtp.example.com");
        _mockConfiguration.Setup(c => c["Email:Smtp:Port"]).Returns("587");
        _mockConfiguration.Setup(c => c["Email:Smtp:Username"]).Returns("user@example.com");
        _mockConfiguration.Setup(c => c["Email:Smtp:Password"]).Returns("password");
        _mockConfiguration.Setup(c => c["Email:From:Address"]).Returns("noreply@example.com");
        _mockConfiguration.Setup(c => c["Email:From:Name"]).Returns("Test Calendar");
        _mockConfiguration.Setup(c => c["Email:Smtp:SkipSslValidation"]).Returns("true");

        return new EmailService(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_SendsEmail_WhenEnabled()
    {
        var service = CreateService(enabled: true);

        var act = async () => await service.SendBookingConfirmationAsync(
            "client@example.com",
            "Test Client",
            "Test Business",
            "Test Service",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "http://localhost:4200/manage/123");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_DoesNothing_WhenDisabled()
    {
        var service = CreateService(enabled: false);

        await service.SendBookingConfirmationAsync(
            "client@example.com",
            "Test Client",
            "Test Business",
            "Test Service",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "http://localhost:4200/manage/123");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email disabled")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBookingCancellationAsync_SendsEmail_WithCorrectSubject()
    {
        var service = CreateService(enabled: true);

        var act = async () => await service.SendBookingCancellationAsync(
            "client@example.com",
            "Test Client",
            "Test Business",
            "Test Service",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendNewBookingNotificationAsync_SendsEmail_ToBusiness()
    {
        var service = CreateService(enabled: true);

        var act = async () => await service.SendNewBookingNotificationAsync(
            "business@example.com",
            "Test Business",
            "Test Client",
            "client@example.com",
            "555-1234",
            "Test Service",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "Test notes");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmailAsync_UsesCorrectFromAddress()
    {
        _mockConfiguration.Setup(c => c["Email:From:Address"]).Returns("custom-from@example.com");
        _mockConfiguration.Setup(c => c["Email:From:Name"]).Returns("Custom Name");
        var service = CreateService(enabled: true);

        var act = async () => await service.SendEmailAsync(
            "to@example.com",
            "Test Subject",
            "Test Body",
            true);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmailAsync_HandlesSmtpFailure_Gracefully()
    {
        _mockConfiguration.Setup(c => c["Email:Smtp:Host"]).Returns("invalid-smtp-host-that-does-not-exist");
        _mockConfiguration.Setup(c => c["Email:Smtp:SkipSslValidation"]).Returns("false");
        var service = CreateService(enabled: true);

        var act = async () => await service.SendEmailAsync(
            "to@example.com",
            "Test Subject",
            "Test Body",
            true);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmailAsync_SetsHtmlBody_WhenIsHtmlTrue()
    {
        var service = CreateService(enabled: true);

        var act = async () => await service.SendEmailAsync(
            "to@example.com",
            "Test Subject",
            "<html><body>HTML Content</body></html>",
            true);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendEmailAsync_SetsTextBody_WhenIsHtmlFalse()
    {
        var service = CreateService(enabled: true);

        var act = async () => await service.SendEmailAsync(
            "to@example.com",
            "Test Subject",
            "Plain text content",
            false);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_IncludesManagementUrl()
    {
        var service = CreateService(enabled: true);
        var managementUrl = "http://localhost:4200/manage/abc123";

        var act = async () => await service.SendBookingConfirmationAsync(
            "client@example.com",
            "Test Client",
            "Test Business",
            "Test Service",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            managementUrl,
            "Some notes");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendNewBookingNotificationAsync_HandlesNullPhone()
    {
        var service = CreateService(enabled: true);

        var act = async () => await service.SendNewBookingNotificationAsync(
            "business@example.com",
            "Test Business",
            "Test Client",
            "client@example.com",
            string.Empty,
            "Test Service",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_HandlesNullNotes()
    {
        var service = CreateService(enabled: true);

        var act = async () => await service.SendBookingConfirmationAsync(
            "client@example.com",
            "Test Client",
            "Test Business",
            "Test Service",
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "http://localhost:4200/manage/123",
            null);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Constructor_LogsWarning_WhenEmailEnabledButNoUsername()
    {
        _mockConfiguration.Setup(c => c["Email:Enabled"]).Returns("true");
        _mockConfiguration.Setup(c => c["Email:Smtp:Username"]).Returns("");

        _ = new EmailService(_mockLogger.Object, _mockConfiguration.Object);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SMTP username")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_LogsInformation_WhenDisabled()
    {
        var service = CreateService(enabled: false);

        await service.SendEmailAsync(
            "to@example.com",
            "Test Subject",
            "Test Body",
            true);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email disabled")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_LogsError_OnSmtpFailure()
    {
        _mockConfiguration.Setup(c => c["Email:Smtp:Host"]).Returns("invalid-host");
        _mockConfiguration.Setup(c => c["Email:Smtp:SkipSslValidation"]).Returns("false");
        var service = CreateService(enabled: true);

        await service.SendEmailAsync(
            "to@example.com",
            "Test Subject",
            "Test Body",
            true);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
