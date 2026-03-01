using CalendarManager.API.Controllers;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<IOAuthService> _mockOAuthService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _context = TestDbContextFactory.Create();
        _mockOAuthService = new Mock<IOAuthService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _controller = new AuthController(_mockOAuthService.Object, _context, _mockConfiguration.Object);
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
    }

    [Fact]
    public void GoogleLogin_Returns200_WithAuthUrlAndState()
    {
        var expectedUrl = "https://accounts.google.com/oauth?client_id=test";
        _mockOAuthService.Setup(s => s.GetAuthorizationUrl(It.IsAny<string>()))
            .Returns(expectedUrl);

        var result = _controller.GoogleLogin();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var authUrl = value?.GetType().GetProperty("authUrl")?.GetValue(value)?.ToString();
        var state = value?.GetType().GetProperty("state")?.GetValue(value)?.ToString();
        
        authUrl.Should().Be(expectedUrl);
        state.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Callback_Returns400_WhenCodeMissing()
    {
        var result = await _controller.Callback(null!, "state");

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Callback_Returns400_WhenStateMissing()
    {
        var result = await _controller.Callback("code", null!);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Callback_CreatesNewUser_WhenNotExists()
    {
        var tokens = new OAuthToken
        {
            UserId = Guid.NewGuid(),
            AccessToken = "encrypted_access",
            RefreshToken = "encrypted_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        
        _mockOAuthService.Setup(s => s.ExchangeCodeForTokensAsync("code", "state", It.IsAny<Guid>()))
            .ReturnsAsync(tokens);
        
        _mockConfiguration.Setup(c => c["Frontend:Url"]).Returns("http://localhost:4200");

        var result = await _controller.Callback("code", "state");

        result.Should().BeOfType<RedirectResult>();
        var user = _context.Users.FirstOrDefault(u => u.Email == "test@example.com");
        user.Should().NotBeNull();
    }

    [Fact]
    public async Task Callback_UpdatesExistingUser_WhenExists()
    {
        var existingUser = new User
        {
            Email = "test@example.com",
            DisplayName = "Existing User",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var tokens = new OAuthToken
        {
            UserId = existingUser.Id,
            AccessToken = "encrypted_access",
            RefreshToken = "encrypted_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        
        _mockOAuthService.Setup(s => s.ExchangeCodeForTokensAsync("code", "state", existingUser.Id))
            .ReturnsAsync(tokens);
        
        _mockConfiguration.Setup(c => c["Frontend:Url"]).Returns("http://localhost:4200");

        var result = await _controller.Callback("code", "state");

        result.Should().BeOfType<RedirectResult>();
        var userCount = _context.Users.Count(u => u.Email == "test@example.com");
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task Status_ReturnsAuthenticated_WhenUserHasTokens()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = new OAuthToken
        {
            UserId = user.Id,
            AccessToken = "encrypted_access",
            RefreshToken = "encrypted_refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        var result = await _controller.Status("test@example.com");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        var authenticated = (bool?)value?.GetType().GetProperty("authenticated")?.GetValue(value);
        authenticated.Should().BeTrue();
    }

    [Fact]
    public async Task Status_ReturnsUnauthenticated_WhenUserNotFound()
    {
        var result = await _controller.Status("nonexistent@example.com");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        var authenticated = (bool?)value?.GetType().GetProperty("authenticated")?.GetValue(value);
        authenticated.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_RevokesTokens_And_Returns200()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockOAuthService.Setup(s => s.RevokeTokensAsync(user.Id))
            .Returns(Task.CompletedTask);

        var result = await _controller.Logout("test@example.com");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        _mockOAuthService.Verify(s => s.RevokeTokensAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task Logout_ReturnsOk_WhenUserNotFound()
    {
        var result = await _controller.Logout("nonexistent@example.com");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task TestToken_ReturnsSuccess_WhenTokenValid()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockOAuthService.Setup(s => s.GetValidAccessTokenAsync(user.Id))
            .ReturnsAsync("ya29.test-token-value");

        var result = await _controller.TestToken("test@example.com");

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        var success = (bool?)value?.GetType().GetProperty("success")?.GetValue(value);
        success.Should().BeTrue();
    }

    [Fact]
    public async Task TestToken_ReturnsBadRequest_WhenUserNotFound()
    {
        var result = await _controller.TestToken("nonexistent@example.com");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TestToken_ReturnsBadRequest_WhenTokenExpired()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "Test User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _mockOAuthService.Setup(s => s.GetValidAccessTokenAsync(user.Id))
            .ThrowsAsync(new InvalidOperationException("Token expired"));

        var result = await _controller.TestToken("test@example.com");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Status_ReturnsBadRequest_WhenEmailEmpty()
    {
        var result = await _controller.Status("");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Logout_ReturnsBadRequest_WhenEmailEmpty()
    {
        var result = await _controller.Logout("");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task TestToken_ReturnsBadRequest_WhenEmailEmpty()
    {
        var result = await _controller.TestToken("");

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
