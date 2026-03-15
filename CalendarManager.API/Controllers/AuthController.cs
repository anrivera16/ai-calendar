using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;

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
            return BadRequest(new { error = "Failed to initiate OAuth flow", details = ex.Message });
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

        try
        {
            _logger.LogInformation("Starting OAuth callback for state: {State}", state);
            
            // First, create a placeholder user with a temporary ID
            // This is needed because OAuthToken has a foreign key to User
            var tempUserId = Guid.NewGuid();
            _logger.LogInformation("Creating placeholder user with temp ID: {TempUserId}", tempUserId);
            
            var placeholderUser = new User
            {
                Id = tempUserId,
                Email = $"placeholder_{tempUserId}@temp.local",
                DisplayName = "Temporary User",
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Users.Add(placeholderUser);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Placeholder user created successfully");
            
            // Exchange authorization code for tokens (will now succeed because user exists)
            var tokens = await _oauthService.ExchangeCodeForTokensAsync(code, state, tempUserId);
            _logger.LogInformation("Tokens exchanged successfully for temp user: {TempUserId}", tempUserId);
            
            // Now get the user's profile info from Google using the access token
            var accessToken = await _oauthService.GetValidAccessTokenAsync(tempUserId);
            var googleUserInfo = await _oauthService.GetUserInfoAsync(accessToken);
            _logger.LogInformation("Got user info from Google: {Email}", googleUserInfo.Email);
            
            // Use the real Google user email to find or create the real user
            var userEmail = googleUserInfo.Email;
            var userDisplayName = googleUserInfo.Name ?? googleUserInfo.Email.Split('@')[0];
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (user == null)
            {
                _logger.LogInformation("Creating new user for email: {Email}", userEmail);
                user = new User
                {
                    Email = userEmail,
                    DisplayName = userDisplayName,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("New user created with ID: {UserId}", user.Id);
            }
            else
            {
                _logger.LogInformation("Existing user found with ID: {UserId}", user.Id);
            }
            
            // Remove any old/stale OAuth tokens for the real user before reassigning
            var staleTokens = await _context.OAuthTokens
                .Where(t => t.UserId == user.Id)
                .ToListAsync();
            if (staleTokens.Any())
            {
                _logger.LogInformation("Removing {Count} stale OAuth tokens for user {UserId}", staleTokens.Count, user.Id);
                _context.OAuthTokens.RemoveRange(staleTokens);
                await _context.SaveChangesAsync();
            }

            // Now update the OAuth token with the correct user ID
            var userToken = await _context.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == tempUserId);
            if (userToken != null)
            {
                _logger.LogInformation("Updating OAuth token user ID from {OldUserId} to {NewUserId}", tempUserId, user.Id);
                userToken.UserId = user.Id;
                await _context.SaveChangesAsync();
                _logger.LogInformation("OAuth token updated successfully");
            }
            
            // Clean up the placeholder user
            var placeholder = await _context.Users.FindAsync(tempUserId);
            if (placeholder != null)
            {
                _context.Users.Remove(placeholder);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Placeholder user removed");
            }

            // Generate JWT token for the user
            var jwtToken = GenerateJwtToken(user);

            // Set JWT as cookie accessible to JS for SPA
            var isLocalhost = Request.Host.Host == "localhost" || Request.Host.Host == "127.0.0.1";
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = !isLocalhost, // Only require HTTPS in non-localhost environments
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddHours(1)
            };
            Response.Cookies.Append("jwt_token", jwtToken, cookieOptions);

            // Redirect to frontend with success message (no token in URL)
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") 
                ?? _configuration["Frontend:Url"] 
                ?? "http://localhost:4200";
            var redirectUrl = $"{frontendUrl}?auth=success&user={Uri.EscapeDataString(user.Email)}";
            
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            // Log the full exception details for debugging
            var innerMessage = ex.InnerException?.Message ?? "No inner exception";
            var innerInnerMessage = ex.InnerException?.InnerException?.Message ?? "No inner inner exception";
            _logger.LogError(ex, "OAuth callback error: {Message}. Inner: {InnerMessage}. InnerInner: {InnerInnerMessage}", 
                ex.Message, innerMessage, innerInnerMessage);
            
            // Redirect to frontend with error message
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") 
                ?? _configuration["Frontend:Url"] 
                ?? "http://localhost:4200";
            var redirectUrl = $"{frontendUrl}?auth=error&message={Uri.EscapeDataString(ex.Message)}";
            
            return Redirect(redirectUrl);
        }
    }

    /// <summary>
    /// Checks the current authentication status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        try
        {
            // Try to get user from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Ok(new { authenticated = false, message = "No authentication token provided" });
            }
            
            var user = await _context.Users
                .Include(u => u.OAuthTokens)
                .FirstOrDefaultAsync(u => u.Id == userId);

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
            return BadRequest(new { error = "Failed to check auth status", details = ex.Message });
        }
    }

    /// <summary>
    /// Logs out the user by revoking OAuth tokens
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest(new { error = "User not authenticated" });
        }

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
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
            return BadRequest(new { error = "Logout failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to verify token refresh works
    /// </summary>
    [HttpPost("test-token")]
    [Authorize]
    public async Task<IActionResult> TestToken()
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest(new { error = "User not authenticated" });
        }

        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
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
                tokenLength = accessToken.Length,
                startsWithBearer = accessToken.StartsWith("ya29") // Google tokens typically start with ya29
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to get access token", details = ex.Message });
        }
    }

    /// <summary>
    /// Generates a JWT token for the authenticated user
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var jwtKey = _configuration["Authentication:Jwt:SecretKey"];
        var jwtIssuer = _configuration["Authentication:Jwt:Issuer"];
        var jwtAudience = _configuration["Authentication:Jwt:Audience"];
        var expirationMinutes = _configuration["Authentication:Jwt:ExpirationMinutes"];

        // JWT_SECRET must be set - fail fast if missing
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT_SECRET environment variable is required. " +
                "Set Authentication:Jwt:SecretKey in configuration or JWT_SECRET environment variable.");
        }
        if (string.IsNullOrEmpty(jwtIssuer))
        {
            jwtIssuer = "AI Calendar Manager";
        }
        if (string.IsNullOrEmpty(jwtAudience))
        {
            jwtAudience = "AI Calendar Manager";
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.DisplayName ?? user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expires = int.TryParse(expirationMinutes, out var minutes) 
            ? DateTime.UtcNow.AddMinutes(minutes) 
            : DateTime.UtcNow.AddHours(1);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}