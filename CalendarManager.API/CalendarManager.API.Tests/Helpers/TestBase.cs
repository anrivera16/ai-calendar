using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Helpers;

public abstract class TestBase : IAsyncLifetime
{
    protected AppDbContext DbContext { get; private set; } = null!;
    protected Mock<ITokenEncryptionService> MockTokenEncryptionService { get; private set; } = null!;
    protected Mock<IOAuthService> MockOAuthService { get; private set; } = null!;
    protected Mock<IGoogleCalendarService> MockGoogleCalendarService { get; private set; } = null!;
    protected Mock<IClaudeService> MockClaudeService { get; private set; } = null!;
    protected Mock<IAvailabilityService> MockAvailabilityService { get; private set; } = null!;
    protected Mock<IBookingService> MockBookingService { get; private set; } = null!;
    protected Mock<IEmailService> MockEmailService { get; private set; } = null!;
    protected TestDataFactory DataFactory { get; private set; } = null!;

    public virtual Task InitializeAsync()
    {
        DbContext = TestDbContextFactory.CreateInMemoryDbContext();
        MockTokenEncryptionService = new Mock<ITokenEncryptionService>();
        MockOAuthService = new Mock<IOAuthService>();
        MockGoogleCalendarService = new Mock<IGoogleCalendarService>();
        MockClaudeService = new Mock<IClaudeService>();
        MockAvailabilityService = new Mock<IAvailabilityService>();
        MockBookingService = new Mock<IBookingService>();
        MockEmailService = new Mock<IEmailService>();
        DataFactory = new TestDataFactory(DbContext);
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        await TestDbContextFactory.DestroyAsync(DbContext);
    }

    protected async Task<User> CreateTestUserAsync(string email = "test@example.com", string? displayName = "Test User")
    {
        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            GoogleUserId = $"google_{Guid.NewGuid():N}"
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    protected async Task<BusinessProfile> CreateTestBusinessProfileAsync(Guid userId, string businessName = "Test Business")
    {
        var profile = new BusinessProfile
        {
            UserId = userId,
            BusinessName = businessName,
            Slug = businessName.ToLower().Replace(" ", "-"),
            IsActive = true
        };
        DbContext.BusinessProfiles.Add(profile);
        await DbContext.SaveChangesAsync();
        return profile;
    }

    protected async Task<Service> CreateTestServiceAsync(Guid businessProfileId, string name = "Test Service", int durationMinutes = 60, decimal price = 100m)
    {
        var service = new Service
        {
            BusinessProfileId = businessProfileId,
            Name = name,
            DurationMinutes = durationMinutes,
            Price = price,
            IsActive = true
        };
        DbContext.Services.Add(service);
        await DbContext.SaveChangesAsync();
        return service;
    }

    protected async Task<Client> CreateTestClientAsync(string name = "Test Client", string email = "client@example.com")
    {
        var client = new Client
        {
            Name = name,
            Email = email
        };
        DbContext.Clients.Add(client);
        await DbContext.SaveChangesAsync();
        return client;
    }

    protected async Task<Booking> CreateTestBookingAsync(Guid businessProfileId, Guid serviceId, Guid clientId, DateTime startTime, DateTime endTime)
    {
        var booking = new Booking
        {
            BusinessProfileId = businessProfileId,
            ServiceId = serviceId,
            ClientId = clientId,
            StartTime = startTime,
            EndTime = endTime,
            Status = BookingStatus.Confirmed
        };
        DbContext.Bookings.Add(booking);
        await DbContext.SaveChangesAsync();
        return booking;
    }

    protected async Task<AvailabilityRule> CreateTestAvailabilityRuleAsync(Guid businessProfileId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        var rule = new AvailabilityRule
        {
            BusinessProfileId = businessProfileId,
            RuleType = RuleType.Weekly,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsAvailable = true
        };
        DbContext.AvailabilityRules.Add(rule);
        await DbContext.SaveChangesAsync();
        return rule;
    }
}

public class TestDataFactory
{
    private readonly AppDbContext _dbContext;

    public TestDataFactory(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public User CreateUser(string email = "test@example.com", string? displayName = "Test User", string? googleUserId = null)
    {
        return new User
        {
            Email = email,
            DisplayName = displayName,
            GoogleUserId = googleUserId ?? $"google_{Guid.NewGuid():N}"
        };
    }

    public BusinessProfile CreateBusinessProfile(Guid userId, string businessName = "Test Business")
    {
        return new BusinessProfile
        {
            UserId = userId,
            BusinessName = businessName,
            Slug = GenerateSlug(businessName),
            IsActive = true
        };
    }

    public Service CreateService(Guid businessProfileId, string name = "Test Service", int durationMinutes = 60, decimal price = 100m)
    {
        return new Service
        {
            BusinessProfileId = businessProfileId,
            Name = name,
            DurationMinutes = durationMinutes,
            Price = price,
            IsActive = true
        };
    }

    public Client CreateClient(string name = "Test Client", string email = "client@example.com", string? phone = null)
    {
        return new Client
        {
            Name = name,
            Email = email,
            Phone = phone
        };
    }

    public Booking CreateBooking(Guid businessProfileId, Guid serviceId, Guid clientId, DateTime startTime, DateTime endTime, BookingStatus status = BookingStatus.Confirmed)
    {
        return new Booking
        {
            BusinessProfileId = businessProfileId,
            ServiceId = serviceId,
            ClientId = clientId,
            StartTime = startTime,
            EndTime = endTime,
            Status = status
        };
    }

    public AvailabilityRule CreateWeeklyAvailabilityRule(Guid businessProfileId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime)
    {
        return new AvailabilityRule
        {
            BusinessProfileId = businessProfileId,
            RuleType = RuleType.Weekly,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsAvailable = true
        };
    }

    public AvailabilityRule CreateDateOverrideRule(Guid businessProfileId, DateTime specificDate, TimeSpan startTime, TimeSpan endTime, bool isAvailable = true)
    {
        return new AvailabilityRule
        {
            BusinessProfileId = businessProfileId,
            RuleType = RuleType.DateOverride,
            SpecificDate = specificDate.Date,
            StartTime = startTime,
            EndTime = endTime,
            IsAvailable = isAvailable
        };
    }

    public AvailabilityRule CreateBreakRule(Guid businessProfileId, DayOfWeek? dayOfWeek, DateTime? specificDate, TimeSpan startTime, TimeSpan endTime)
    {
        return new AvailabilityRule
        {
            BusinessProfileId = businessProfileId,
            RuleType = RuleType.Break,
            DayOfWeek = dayOfWeek,
            SpecificDate = specificDate,
            StartTime = startTime,
            EndTime = endTime,
            IsAvailable = false
        };
    }

    public OAuthToken CreateOAuthToken(Guid userId, string accessToken = "encrypted_access_token", string refreshToken = "encrypted_refresh_token", DateTime? expiresAt = null)
    {
        return new OAuthToken
        {
            UserId = userId,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddHours(1),
            Scope = "https://www.googleapis.com/auth/calendar",
            TokenType = "Bearer"
        };
    }

    public Conversation CreateConversation(Guid userId, string? title = "Test Conversation")
    {
        return new Conversation
        {
            UserId = userId,
            Title = title
        };
    }

    public Message CreateMessage(Guid conversationId, string role, string content)
    {
        return new Message
        {
            ConversationId = conversationId,
            Role = role,
            Content = content
        };
    }

    private static string GenerateSlug(string businessName)
    {
        return businessName.ToLower().Replace(" ", "-").Replace(".", "");
    }
}
