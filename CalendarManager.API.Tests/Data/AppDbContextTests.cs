using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CalendarManager.API.Tests.Data;

public class AppDbContextTests : IDisposable
{
    private readonly AppDbContext _context;

    public AppDbContextTests()
    {
        _context = TestDbContextFactory.Create();
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
    }

    [Fact]
    public async Task CanCreateUser_WithRequiredFields()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("test@example.com");
        savedUser.DisplayName.Should().Be("Test User");
        savedUser.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task UserEmail_MustBeUnique()
    {
        var user1 = new User { Email = "duplicate@example.com" };
        var user2 = new User { Email = "duplicate@example.com" };

        _context.Users.Add(user1);
        await _context.SaveChangesAsync();

        _context.Users.Add(user2);

        var uniqueIndex = _context.Model.FindEntityType(typeof(User))?
            .GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Any(p => p.Name == "Email"));

        uniqueIndex.Should().NotBeNull("Email column should have a unique index");
    }

    [Fact]
    public async Task CreatedAt_IsAutoSet_OnInsert()
    {
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);
        var businessProfile = new BusinessProfile
        {
            UserId = Guid.NewGuid(),
            BusinessName = "Test Business",
            Slug = "test-business"
        };

        _context.BusinessProfiles.Add(businessProfile);
        await _context.SaveChangesAsync();
        var afterCreate = DateTime.UtcNow.AddSeconds(1);

        businessProfile.CreatedAt.Should().BeAfter(beforeCreate);
        businessProfile.CreatedAt.Should().BeBefore(afterCreate);
    }

    [Fact]
    public async Task UpdatedAt_IsAutoSet_OnSaveChanges()
    {
        var service = new Service
        {
            Name = "Test Service",
            DurationMinutes = 60,
            Price = 100
        };
        var businessProfile = new BusinessProfile
        {
            UserId = Guid.NewGuid(),
            BusinessName = "Test Business",
            Slug = "test-business",
            Services = new List<Service> { service }
        };

        _context.BusinessProfiles.Add(businessProfile);
        await _context.SaveChangesAsync();

        var initialUpdatedAt = service.UpdatedAt;
        await Task.Delay(50);

        service.Name = "Updated Service";
        await _context.SaveChangesAsync();

        service.UpdatedAt.Should().BeAfter(initialUpdatedAt);
    }

    [Fact]
    public async Task CascadeDelete_User_DeletesOAuthTokens()
    {
        var user = new User { Email = "cascade-oauth@example.com" };
        var oauthToken = new OAuthToken
        {
            User = user,
            AccessToken = "encrypted-access",
            RefreshToken = "encrypted-refresh",
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        _context.Users.Add(user);
        _context.OAuthTokens.Add(oauthToken);
        await _context.SaveChangesAsync();

        var tokenId = oauthToken.Id;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        var deletedToken = await _context.OAuthTokens.FindAsync(tokenId);
        deletedToken.Should().BeNull();
    }

    [Fact]
    public async Task CascadeDelete_User_DeletesConversations()
    {
        var user = new User { Email = "cascade-conv@example.com" };
        var conversation = new Conversation
        {
            User = user,
            Title = "Test Conversation"
        };

        _context.Users.Add(user);
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        var conversationId = conversation.Id;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        var deletedConversation = await _context.Conversations.FindAsync(conversationId);
        deletedConversation.Should().BeNull();
    }

    [Fact]
    public async Task CascadeDelete_BusinessProfile_DeletesServices()
    {
        var user = new User { Email = "cascade-services@example.com" };
        var businessProfile = new BusinessProfile
        {
            User = user,
            BusinessName = "Test Business",
            Slug = "test-business-services"
        };
        var service = new Service
        {
            BusinessProfile = businessProfile,
            Name = "Test Service",
            DurationMinutes = 60,
            Price = 100
        };

        _context.Users.Add(user);
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var serviceId = service.Id;
        _context.BusinessProfiles.Remove(businessProfile);
        await _context.SaveChangesAsync();

        var deletedService = await _context.Services.FindAsync(serviceId);
        deletedService.Should().BeNull();
    }

    [Fact]
    public async Task CascadeDelete_BusinessProfile_DeletesAvailabilityRules()
    {
        var user = new User { Email = "cascade-avail@example.com" };
        var businessProfile = new BusinessProfile
        {
            User = user,
            BusinessName = "Test Business",
            Slug = "test-business-avail"
        };
        var availabilityRule = new AvailabilityRule
        {
            BusinessProfile = businessProfile,
            RuleType = RuleType.Weekly,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(17)
        };

        _context.Users.Add(user);
        _context.AvailabilityRules.Add(availabilityRule);
        await _context.SaveChangesAsync();

        var ruleId = availabilityRule.Id;
        _context.BusinessProfiles.Remove(businessProfile);
        await _context.SaveChangesAsync();

        var deletedRule = await _context.AvailabilityRules.FindAsync(ruleId);
        deletedRule.Should().BeNull();
    }

    [Fact]
    public void RestrictDelete_Service_WhenBookingsExist()
    {
        var entityType = _context.Model.FindEntityType(typeof(Booking));
        var foreignKey = entityType?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType?.ClrType == typeof(Service));

        foreignKey.Should().NotBeNull();
        foreignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void RestrictDelete_Client_WhenBookingsExist()
    {
        var entityType = _context.Model.FindEntityType(typeof(Booking));
        var foreignKey = entityType?.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType?.ClrType == typeof(Client));

        foreignKey.Should().NotBeNull();
        foreignKey!.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public async Task BookingStatus_DefaultsToConfirmed()
    {
        var user = new User { Email = "booking-status@example.com" };
        var businessProfile = new BusinessProfile
        {
            User = user,
            BusinessName = "Test Business",
            Slug = "test-business-status"
        };
        var service = new Service
        {
            BusinessProfile = businessProfile,
            Name = "Test Service",
            DurationMinutes = 60,
            Price = 100
        };
        var client = new Client
        {
            Name = "Test Client",
            Email = "client-status@example.com"
        };
        var booking = new Booking
        {
            BusinessProfile = businessProfile,
            Service = service,
            Client = client,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1)
        };

        _context.Users.Add(user);
        _context.Clients.Add(client);
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Business_Slug_MustBeUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(BusinessProfile));
        var slugIndex = entityType?.GetIndexes()
            .FirstOrDefault(i => i.IsUnique && i.Properties.Any(p => p.Name == "Slug"));

        slugIndex.Should().NotBeNull("Slug column should have a unique index");
    }
}
