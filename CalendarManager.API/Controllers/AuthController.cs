using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IOAuthService oauthService, AppDbContext context, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _oauthService = oauthService;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Initiates Google OAuth flow by redirecting to Google's authorization page
    /// </summary>
    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        try
        {
            // Generate random state parameter for CSRF protection
            var state = Guid.NewGuid().ToString("N");
            
            // Get Google authorization URL with PKCE
            var authUrl = _oauthService.GetAuthorizationUrl(state);
            
            return Ok(new { authUrl, state });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate OAuth flow");
            return BadRequest(new { error = "Failed to initiate OAuth flow" });
        }
    }

    /// <summary>
    /// Handles the OAuth callback from Google with authorization code
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return BadRequest(new { error = "Missing authorization code or state parameter" });
        }

        // Validate state parameter length to prevent injection
        if (state.Length != 32 || !state.All(c => char.IsLetterOrDigit(c)))
        {
            _logger.LogWarning("Invalid state parameter received in OAuth callback");
            return BadRequest(new { error = "Invalid state parameter" });
        }

        try
        {
            // For demo purposes, we'll create a test user or find existing one
            // In a real app, you'd get the user ID from the session or JWT token
            var testEmail = "test@example.com";
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == testEmail);
            if (user == null)
            {
                user = new User
                {
                    Email = testEmail,
                    DisplayName = "Test User",
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            // Exchange authorization code for tokens
            var tokens = await _oauthService.ExchangeCodeForTokensAsync(code, state, user.Id);

            // Redirect to frontend with success message
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") 
                ?? _configuration["Frontend:Url"] 
                ?? "http://localhost:4201";
            var redirectUrl = $"{frontendUrl}?auth=success&user={Uri.EscapeDataString(user.Email)}";
            
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth callback failed");
            // Redirect to frontend with error message (sanitized)
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") 
                ?? _configuration["Frontend:Url"] 
                ?? "http://localhost:4201";
            var redirectUrl = $"{frontendUrl}?auth=error&message={Uri.EscapeDataString("Authentication failed")}";
            
            return Redirect(redirectUrl);
        }
    }

    /// <summary>
    /// Checks the current authentication status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] string? userEmail = null)
    {
        // Security: Require userEmail parameter instead of using default
        if (string.IsNullOrEmpty(userEmail))
        {
            return Ok(new { authenticated = false, message = "No user specified" });
        }

        // Validate email format to prevent injection
        if (!IsValidEmail(userEmail))
        {
            return BadRequest(new { error = "Invalid email format" });
        }

        try
        {
            var user = await _context.Users
                .Include(u => u.OAuthTokens)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return Ok(new { authenticated = false, message = "User not found" });
            }

            var hasValidToken = user.OAuthTokens.Any(t => t.ExpiresAt > DateTime.UtcNow);

            return Ok(new 
            { 
                authenticated = hasValidToken,
                user = new { user.Id, user.Email, user.DisplayName },
                tokenCount = user.OAuthTokens.Count,
                nextExpiry = user.OAuthTokens.FirstOrDefault()?.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check auth status for user {UserEmail}", userEmail);
            return BadRequest(new { error = "Failed to check auth status" });
        }
    }

    /// <summary>
    /// Logs out the user by revoking OAuth tokens
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromQuery] string? userEmail = null)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return BadRequest(new { error = "User email is required" });
        }

        // Validate email format
        if (!IsValidEmail(userEmail))
        {
            return BadRequest(new { error = "Invalid email format" });
        }

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return Ok(new { success = true, message = "User not found, nothing to logout" });
            }

            // Revoke tokens with Google and remove from database
            await _oauthService.RevokeTokensAsync(user.Id);

            return Ok(new { success = true, message = "Successfully logged out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed for user {UserEmail}", userEmail);
            return BadRequest(new { error = "Logout failed" });
        }
    }

    /// <summary>
    /// Test endpoint to verify token refresh works
    /// </summary>
    [HttpPost("test-token")]
    public async Task<IActionResult> TestToken([FromQuery] string? userEmail = null)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return BadRequest(new { error = "User email is required" });
        }

        // Validate email format
        if (!IsValidEmail(userEmail))
        {
            return BadRequest(new { error = "Invalid email format" });
        }

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                return BadRequest(new { error = "User not found" });
            }

            // This will automatically refresh the token if it's expired
            var accessToken = await _oauthService.GetValidAccessTokenAsync(user.Id);
            
            // Don't return the actual token for security, just confirm it works
            return Ok(new 
            { 
                success = true, 
                message = "Access token retrieved successfully",
                tokenLength = accessToken.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get access token for user {UserEmail}", userEmail);
            return BadRequest(new { error = "Failed to get access token" });
        }
    }

    /// <summary>
    /// Validates email format using simple regex pattern
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        // Simple email validation - more restrictive than necessary but safe
        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return System.Text.RegularExpressions.Regex.IsMatch(email, emailPattern);
    }
}