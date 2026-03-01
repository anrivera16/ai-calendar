using CalendarManager.API.Data;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Override config values with environment variables for cloud deployment
builder.Configuration.AddEnvironmentVariables();

// Set OAuth configuration from environment variables if available
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
var redirectUri = Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI");
var claudeApiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

// Email configuration from environment variables
var emailEnabled = Environment.GetEnvironmentVariable("EMAIL_ENABLED");
var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
var smtpPort = Environment.GetEnvironmentVariable("SMTP_PORT");
var smtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME");
var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
var emailFromAddress = Environment.GetEnvironmentVariable("EMAIL_FROM_ADDRESS");
var emailFromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME");

if (!string.IsNullOrEmpty(googleClientId))
    builder.Configuration["Authentication:Google:ClientId"] = googleClientId;
if (!string.IsNullOrEmpty(googleClientSecret))
    builder.Configuration["Authentication:Google:ClientSecret"] = googleClientSecret;
if (!string.IsNullOrEmpty(encryptionKey))
    builder.Configuration["Authentication:Encryption:Key"] = encryptionKey;
if (!string.IsNullOrEmpty(redirectUri))
    builder.Configuration["Authentication:Google:RedirectUri"] = redirectUri;
if (!string.IsNullOrEmpty(claudeApiKey))
    builder.Configuration["Claude:ApiKey"] = claudeApiKey;

// Override email config from environment variables
if (!string.IsNullOrEmpty(emailEnabled))
    builder.Configuration["Email:Enabled"] = emailEnabled;
if (!string.IsNullOrEmpty(smtpHost))
    builder.Configuration["Email:Smtp:Host"] = smtpHost;
if (!string.IsNullOrEmpty(smtpPort))
    builder.Configuration["Email:Smtp:Port"] = smtpPort;
if (!string.IsNullOrEmpty(smtpUsername))
    builder.Configuration["Email:Smtp:Username"] = smtpUsername;
if (!string.IsNullOrEmpty(smtpPassword))
    builder.Configuration["Email:Smtp:Password"] = smtpPassword;
if (!string.IsNullOrEmpty(emailFromAddress))
    builder.Configuration["Email:From:Address"] = emailFromAddress;
if (!string.IsNullOrEmpty(emailFromName))
    builder.Configuration["Email:From:Name"] = emailFromName;

// Demo mode configuration - set to false to block admin access in production
var demoMode = Environment.GetEnvironmentVariable("DEMO_MODE");
if (!string.IsNullOrEmpty(demoMode))
    builder.Configuration["DemoMode"] = demoMode;

// Add services to the container.
builder.Services.AddControllers();

// Rate limiting configuration
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 30,
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/book/*",
            Period = "5m",
            Limit = 10,
        }
    };
});
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:3000", "http://localhost:4200", "http://localhost:5173", "http://localhost:8080" };
            
            // Add production frontend URL from environment variable
            var productionUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
            if (!string.IsNullOrEmpty(productionUrl))
            {
                allowedOrigins = allowedOrigins.Append(productionUrl).ToArray();
            }
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Database configuration - prioritize environment variable for Docker
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string not found. Set ConnectionStrings:DefaultConnection in appsettings.json or DATABASE_URL environment variable.");
}

// Parse DATABASE_URL format for cloud providers like Heroku/Railway
if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]}";
    
    Console.WriteLine("[DEBUG] Converted DATABASE_URL to standard connection string format");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// HTTP Client for external API calls
builder.Services.AddHttpClient();

// Register application services
builder.Services.AddScoped<ITokenEncryptionService, TokenEncryptionService>();
builder.Services.AddScoped<IOAuthService, OAuthService>();
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
builder.Services.AddScoped<IClaudeService, ClaudeService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "database");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply database migrations automatically on startup (for cloud deployment)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowSpecificOrigins");

// Use rate limiting
app.UseIpRateLimiting();

app.UseRouting();

// Map health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
