using CalendarManager.API.Data;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Services.Implementations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Override config values with environment variables for cloud deployment
builder.Configuration.AddEnvironmentVariables();

// Set OAuth configuration from environment variables if available
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
var redirectUri = Environment.GetEnvironmentVariable("GOOGLE_REDIRECT_URI");
var claudeApiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

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

// Add services to the container.
builder.Services.AddControllers();

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
// Note: Connection string is NOT logged to avoid exposing credentials
if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    try
    {
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]}";
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Failed to parse DATABASE_URL. Ensure it's in the correct format.", ex);
    }
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

// Security headers middleware
app.Use(async (context, next) =>
{
    // Prevent clickjacking
    context.Response.Headers["X-Frame-Options"] = "DENY";
    
    // Prevent MIME type sniffing
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    
    // Enable XSS protection in browsers
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    
    // Control referrer information
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';";
    
    // Permissions Policy (formerly Feature Policy)
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    
    await next();
});

// Use CORS
app.UseCors("AllowSpecificOrigins");

app.UseRouting();

// Map health check endpoint
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
