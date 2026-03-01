namespace CalendarManager.API.Services.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends a booking confirmation email to the client
    /// </summary>
    Task SendBookingConfirmationAsync(
        string toEmail,
        string clientName,
        string businessName,
        string serviceName,
        DateTime startTime,
        DateTime endTime,
        string managementUrl,
        string? notes = null);

    /// <summary>
    /// Sends a booking cancellation email to the client
    /// </summary>
    Task SendBookingCancellationAsync(
        string toEmail,
        string clientName,
        string businessName,
        string serviceName,
        DateTime startTime,
        DateTime endTime);

    /// <summary>
    /// Sends a notification to the business owner about a new booking
    /// </summary>
    Task SendNewBookingNotificationAsync(
        string toEmail,
        string businessName,
        string clientName,
        string clientEmail,
        string clientPhone,
        string serviceName,
        DateTime startTime,
        DateTime endTime,
        string? notes = null);

    /// <summary>
    /// Sends a generic email
    /// </summary>
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}
