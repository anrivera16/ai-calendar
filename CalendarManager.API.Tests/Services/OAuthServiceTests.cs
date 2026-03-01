using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models;
using CalendarManager.API.Services.Implementations;
using CalendarManager.API.Services.Interfaces;
using CalendarManager.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Xunit;

namespace CalendarManager.API.Tests.Services;

public class OAuthServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Mock<ITokenEncryptionService> _mockEncryptionService;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private OAuthService _service;
    private readonly User _testUser;

    public OAuthServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _mockEncryptionService = new Mock<ITokenEncryptionService>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
        _configuration = CreateTestConfiguration();

        SetupEncryptionService();

        _service = new OAuthService(
            _configuration,
            _context,
            _mockEncryptionService.Object,
            _httpClient);

        _testUser = new User { Email = "test@example.com" };
        _context.Users.Add(_testUser);
        _context.SaveChanges();
    }

    private static IConfiguration CreateTestConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-client-id",
                ["Authentication:Google:ClientSecret"] = "test-client-secret",
                ["Authentication:Google:RedirectUri"] = "http://localhost:4200/auth/callback",
                ["Authentication:Google:Scopes:0"] = "openid",
                ["Authentication:Google:Scopes:1"] = "email",
                ["Authentication:Google:Scopes:2"] = "profile",
                ["Authentication:Google:Scopes:3"] = "https://www.googleapis.com/auth/calendar"
            })
            .Build();
        return config;
    }

    private void SetupEncryptionService()
    {
        _mockEncryptionService.Setup(e => e.Encrypt(It.IsAny<string>())).Returns((string s) => $"encrypted_{s}");
        _mockEncryptionService.Setup(e => e.Decrypt(It.IsAny<string>())).Returns((string s) => s.Replace("encrypted_", ""));
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
        _httpClient.Dispose();
    }

    [Fact]
    public void GetAuthorizationUrl_ReturnsValidUrl_WithRequiredParams()
    {
        var state = "test-state-123";

        var result = _service.GetAuthorizationUrl(state);

        result.Should().Contain("accounts.google.com/o/oauth2/v2/auth");
        result.Should().Contain("client_id=test-client-id");
        result.Should().Contain("redirect_uri=");
        result.Should().Contain("response_type=code");
        result.Should().Contain("scope=");
        result.Should().Contain("state=test-state-123");
        result.Should().Contain("code_challenge=");
        result.Should().Contain("code_challenge_method=S256");
        result.Should().Contain("access_type=offline");
        result.Should().Contain("prompt=consent");
    }

    [Fact]
    public void GetAuthorizationUrl_StoresCodeVerifier_ForState()
    {
        var state = "test-state-456";

        _service.GetAuthorizationUrl(state);

        var verifier = _service.RetrieveCodeVerifier(state);
        verifier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void StoreCodeVerifier_And_RetrieveCodeVerifier_RoundTrip()
    {
        var state = "test-state-789";
        var verifier = "test-verifier-value";

        _service.StoreCodeVerifier(state, verifier);
        var result = _service.RetrieveCodeVerifier(state);

        result.Should().Be(verifier);
    }

    [Fact]
    public void RetrieveCodeVerifier_ReturnsNull_ForUnknownState()
    {
        var result = _service.RetrieveCodeVerifier("unknown-state");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExchangeCodeForTokensAsync_ReturnsTokens_OnSuccess()
    {
        var state = "exchange-state";
        var code = "auth-code-123";
        _service.StoreCodeVerifier(state, "verifier-123");

        var tokenResponse = new GoogleTokenResponse
        {
            AccessToken = "access-token-123",
            RefreshToken = "refresh-token-123",
            ExpiresIn = 3600,
            TokenType = "Bearer",
            Scope = "openid email"
        };

        SetupHttpResponse(tokenResponse, HttpStatusCode.OK);

        var result = await _service.ExchangeCodeForTokensAsync(code, state, _testUser.Id);

        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUser.Id);
        result.AccessToken.Should().Be("encrypted_access-token-123");
        result.RefreshToken.Should().Be("encrypted_refresh-token-123");
    }

    [Fact]
    public async Task ExchangeCodeForTokensAsync_ThrowsOnGoogleError()
    {
        var state = "error-state";
        _service.StoreCodeVerifier(state, "verifier");

        SetupHttpResponse(new { error = "invalid_grant" }, HttpStatusCode.BadRequest);

        var act = async () => await _service.ExchangeCodeForTokensAsync("code", state, _testUser.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Token exchange failed*");
    }

    [Fact]
    public async Task ExchangeCodeForTokensAsync_ThrowsOnInvalidState()
    {
        var act = async () => await _service.ExchangeCodeForTokensAsync("code", "unknown-state", _testUser.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid or expired authorization state*");
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_ReturnsExistingToken_WhenNotExpired()
    {
        var token = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_valid-access-token",
            RefreshToken = "encrypted_refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        var result = await _service.GetValidAccessTokenAsync(_testUser.Id);

        result.Should().Be("valid-access-token");
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_RefreshesToken_WhenExpiringSoon()
    {
        var token = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_old-access-token",
            RefreshToken = "encrypted_refresh-token",
            ExpiresAt = DateTime.UtcNow.AddMinutes(3)
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        var tokenResponse = new GoogleTokenResponse
        {
            AccessToken = "new-access-token",
            ExpiresIn = 3600,
            TokenType = "Bearer"
        };
        SetupHttpResponse(tokenResponse, HttpStatusCode.OK);

        var result = await _service.GetValidAccessTokenAsync(_testUser.Id);

        result.Should().Be("new-access-token");
    }

    [Fact]
    public async Task GetValidAccessTokenAsync_ThrowsWhenNoTokenFound()
    {
        var act = async () => await _service.GetValidAccessTokenAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No OAuth token found*");
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_UpdatesTokenInDb()
    {
        var token = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_old-token",
            RefreshToken = "encrypted_refresh-token",
            ExpiresAt = DateTime.UtcNow
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        var tokenResponse = new GoogleTokenResponse
        {
            AccessToken = "new-access-token",
            ExpiresIn = 7200,
            TokenType = "Bearer"
        };
        SetupHttpResponse(tokenResponse, HttpStatusCode.OK);

        var result = await _service.RefreshAccessTokenAsync(token);

        result.AccessToken.Should().Be("encrypted_new-access-token");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddHours(1));
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_ThrowsOnGoogleError()
    {
        var token = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_token",
            RefreshToken = "encrypted_refresh-token",
            ExpiresAt = DateTime.UtcNow
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        SetupHttpResponse(new { error = "invalid_grant" }, HttpStatusCode.BadRequest);

        var act = async () => await _service.RefreshAccessTokenAsync(token);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Token refresh failed*");
    }

    [Fact]
    public async Task RevokeTokensAsync_DeletesTokensFromDb()
    {
        var token = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_token",
            RefreshToken = "encrypted_refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        SetupHttpResponse(new { }, HttpStatusCode.OK);

        await _service.RevokeTokensAsync(_testUser.Id);

        var tokens = await _context.OAuthTokens.ToListAsync();
        tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeTokensAsync_HandlesRevokeEndpointFailure_Gracefully()
    {
        var token = new OAuthToken
        {
            UserId = _testUser.Id,
            AccessToken = "encrypted_token",
            RefreshToken = "encrypted_refresh-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.OAuthTokens.Add(token);
        await _context.SaveChangesAsync();

        SetupHttpResponse(new { error = "error" }, HttpStatusCode.BadRequest);

        await _service.RevokeTokensAsync(_testUser.Id);

        var tokens = await _context.OAuthTokens.ToListAsync();
        tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeTokensAsync_DoesNothing_WhenNoTokenExists()
    {
        var act = async () => await _service.RevokeTokensAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void GetAuthorizationUrl_Throws_WhenConfigurationMissing()
    {
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        
        var service = new OAuthService(
            emptyConfig,
            _context,
            _mockEncryptionService.Object,
            _httpClient);

        var act = () => service.GetAuthorizationUrl("state");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Google OAuth configuration is missing*");
    }

    private void SetupHttpResponse<T>(T content, HttpStatusCode statusCode)
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
            .ReturnsAsync(response);
    }
}
