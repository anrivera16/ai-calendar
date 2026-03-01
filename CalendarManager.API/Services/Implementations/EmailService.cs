using CalendarManager.API.Services.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CalendarManager.API.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly bool _isEnabled;
    private readonly bool _skipSslValidation;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        _isEnabled = configuration["Email:Enabled"] == "true";
        _smtpHost = configuration["Email:Smtp:Host"] ?? "smtp.gmail.com";
        _smtpPort = int.Parse(configuration["Email:Smtp:Port"] ?? "587");
        _smtpUsername = configuration["Email:Smtp:Username"] ?? "";
        _smtpPassword = configuration["Email:Smtp:Password"] ?? "";
        _fromEmail = configuration["Email:From:Address"] ?? "noreply@example.com";
        _fromName = configuration["Email:From:Name"] ?? "AI Calendar";
        _skipSslValidation = configuration["Email:Smtp:SkipSslValidation"] == "true";
        
        if (_isEnabled && string.IsNullOrEmpty(_smtpUsername))
        {
            _logger.LogWarning("Email is enabled but SMTP username is not configured");
        }
    }

    public async Task SendBookingConfirmationAsync(
        string toEmail,
        string clientName,
        string businessName,
        string serviceName,
        DateTime startTime,
        DateTime endTime,
        string managementUrl,
        string? notes = null)
    {
        var subject = $"Booking Confirmed: {serviceName} at {businessName}";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #4F46E5; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 20px; }}
        .details {{ background: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Booking Confirmed!</h1>
        </div>
        <div class='content'>
            <p>Hi {clientName},</p>
            <p>Your booking has been confirmed. Here are the details:</p>
            <div class='details'>
                <p><strong>Service:</strong> {serviceName}</p>
                <p><strong>Business:</strong> {businessName}</p>
                <p><strong>Date:</strong> {startTime:dddd, MMMM d, yyyy}</p>
                <p><strong>Time:</strong> {startTime:h:mm tt} - {endTime:h:mm tt}</p>
                {(string.IsNullOrEmpty(notes) ? "" : $"<p><strong>Notes:</strong> {notes}</p>")}
            </div>
            <p>Need to make changes or cancel? Use the link below:</p>
            <p><a href='{managementUrl}'>{managementUrl}</a></p>
        </div>
        <div class='footer'>
            <p>This email was sent by {businessName} via AI Calendar Manager</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body, true);
    }

    public async Task SendBookingCancellationAsync(
        string toEmail,
        string clientName,
        string businessName,
        string serviceName,
        DateTime startTime,
        DateTime endTime)
    {
        var subject = $"Booking Cancelled: {serviceName} at {businessName}";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #DC2626; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 20px; }}
        .details {{ background: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Booking Cancelled</h1>
        </div>
        <div class='content'>
            <p>Hi {clientName},</p>
            <p>Your booking has been cancelled. Here are the details:</p>
            <div class='details'>
                <p><strong>Service:</strong> {serviceName}</p>
                <p><strong>Business:</strong> {businessName}</p>
                <p><strong>Date:</strong> {startTime:dddd, MMMM d, yyyy}</p>
                <p><strong>Time:</strong> {startTime:h:mm tt} - {endTime:h:mm tt}</p>
            </div>
            <p>If you'd like to rebook, please visit the business website.</p>
        </div>
        <div class='footer'>
            <p>This email was sent by {businessName} via AI Calendar Manager</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body, true);
    }

    public async Task SendNewBookingNotificationAsync(
        string toEmail,
        string businessName,
        string clientName,
        string clientEmail,
        string clientPhone,
        string serviceName,
        DateTime startTime,
        DateTime endTime,
        string? notes = null)
    {
        var subject = $"New Booking: {clientName} - {serviceName}";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #059669; color: white; padding: 20px; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 20px; }}
        .details {{ background: white; padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>New Booking Received!</h1>
        </div>
        <div class='content'>
            <p>Hi,</p>
            <p>You have a new booking. Here are the details:</p>
            <div class='details'>
                <p><strong>Customer:</strong> {clientName}</p>
                <p><strong>Email:</strong> {clientEmail}</p>
                <p><strong>Phone:</strong> {(string.IsNullOrEmpty(clientPhone) ? "Not provided" : clientPhone)}</p>
                <p><strong>Service:</strong> {serviceName}</p>
                <p><strong>Date:</strong> {startTime:dddd, MMMM d, yyyy}</p>
                <p><strong>Time:</strong> {startTime:h:mm tt} - {endTime:h:mm tt}</p>
                {(string.IsNullOrEmpty(notes) ? "" : $"<p><strong>Notes:</strong> {notes}</p>")}
            </div>
        </div>
        <div class='footer'>
            <p>AI Calendar Manager</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body, true);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        if (!_isEnabled)
        {
            _logger.LogInformation("Email disabled. Would have sent to {ToEmail}: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            if (isHtml)
            {
                message.Body = new TextPart("html")
                {
                    Text = body
                };
            }
            else
            {
                message.Body = new TextPart("plain")
                {
                    Text = body
                };
            }

            using var client = new SmtpClient();
            
            // Only skip SSL validation if explicitly configured (e.g., local development)
            if (_skipSslValidation)
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                _logger.LogWarning("SSL certificate validation is disabled. This should only be used for local development.");
            }
            
            await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtpUsername, _smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {ToEmail}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}: {Subject}", toEmail, subject);
            // Don't throw - email failure shouldn't break the booking flow
        }
    }
}
