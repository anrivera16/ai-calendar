using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models;
using CalendarManager.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CalendarManager.API.Services.Implementations;

public class OAuthService : IOAuthService
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly ITokenEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;
    
    // In-memory storage for PKCE code verifiers (use Redis in production)
    private readonly ConcurrentDictionary<string, string> _codeVerifiers = new();

    public OAuthService(
        IConfiguration configuration,
        AppDbContext context,
        ITokenEncryptionService encryptionService,
        HttpClient httpClient)
    {
        _configuration = configuration;
        _context = context;
        _encryptionService = encryptionService;
        _httpClient = httpClient;
    }

    public string GetAuthorizationUrl(string state)
    {
        // Get Google OAuth configuration from appsettings.json
        var clientId = _configuration["Authentication:Google:ClientId"];
        var redirectUri = _configuration["Authentication:Google:RedirectUri"];
        var scopes = _configuration.GetSection("Authentication:Google:Scopes").Get<string[]>();
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri) || scopes == null)
        {
            throw new InvalidOperationException("Google OAuth configuration is missing or incomplete");
        }
        
        // Generate PKCE code verifier and challenge
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        
        // Store code verifier for later verification (when exchanging code for tokens)
        StoreCodeVerifier(state, codeVerifier);
        
        // Build Google OAuth2 authorization URL
        var scopesString = string.Join(" ", scopes);
        var authUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(scopesString)}" +
            $"&access_type=offline" +        // Request refresh token
            $"&prompt=consent" +             // Force consent screen to get refresh token
            $"&state={Uri.EscapeDataString(state)}" +              // CSRF protection
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            $"&code_challenge_method=S256";   // SHA256 challenge method
        
        return authUrl;
    }

    public async Task<OAuthToken> ExchangeCodeForTokensAsync(string code, string state, Guid userId)
    {
        // Retrieve and validate PKCE code verifier
        var codeVerifier = RetrieveCodeVerifier(state);
        if (string.IsNullOrEmpty(codeVerifier))
        {
            throw new InvalidOperationException("Invalid or expired authorization state");
        }
        
        // Get OAuth configuration
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        var redirectUri = _configuration["Authentication:Google:RedirectUri"];
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
        {
            throw new InvalidOperationException("Google OAuth configuration is missing");
        }
        
        // Build token exchange request
        var tokenRequest = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier
        };
        
        try
        {
            // Make token exchange request to Google
            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(tokenRequest));
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token exchange failed: {responseContent}");
            }
            
            // Parse token response
            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
            if (tokenResponse?.AccessToken == null || tokenResponse.RefreshToken == null)
            {
                throw new InvalidOperationException("Invalid token response from Google");
            }
            
            // Calculate expiration time
            var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            
            // Encrypt tokens before storage
            var encryptedAccessToken = _encryptionService.Encrypt(tokenResponse.AccessToken);
            var encryptedRefreshToken = _encryptionService.Encrypt(tokenResponse.RefreshToken);
            
            // Create and save OAuth token entity
            var oauthToken = new OAuthToken
            {
                UserId = userId,
                AccessToken = encryptedAccessToken,
                RefreshToken = encryptedRefreshToken,
                ExpiresAt = expiresAt,
                Scope = tokenResponse.Scope,
                TokenType = tokenResponse.TokenType ?? "Bearer"
            };
            
            // Remove any existing tokens for this user
            var existingTokens = await _context.OAuthTokens
                .Where(t => t.UserId == userId)
                .ToListAsync();
            
            if (existingTokens.Any())
            {
                _context.OAuthTokens.RemoveRange(existingTokens);
            }
            
            // Add new token
            _context.OAuthTokens.Add(oauthToken);
            await _context.SaveChangesAsync();
            
            return oauthToken;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to communicate with Google OAuth service", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid response from Google OAuth service", ex);
        }
    }

    public async Task<string> GetValidAccessTokenAsync(Guid userId)
    {
        // Find user's OAuth token in database
        var tokenEntity = await _context.OAuthTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);
        
        if (tokenEntity == null)
        {
            throw new InvalidOperationException("No OAuth token found for user. Please authenticate with Google first.");
        }
        
        // Check if token expires soon (refresh 5 minutes early to be safe)
        if (tokenEntity.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
        {
            // Token is expired or expiring soon, refresh it
            tokenEntity = await RefreshAccessTokenAsync(tokenEntity);
        }
        
        // Decrypt and return the access token
        return _encryptionService.Decrypt(tokenEntity.AccessToken);
    }

    public async Task<OAuthToken> RefreshAccessTokenAsync(OAuthToken tokenEntity)
    {
        // Decrypt the refresh token
        var refreshToken = _encryptionService.Decrypt(tokenEntity.RefreshToken);
        
        // Get OAuth configuration
        var clientId = _configuration["Authentication:Google:ClientId"];
        var clientSecret = _configuration["Authentication:Google:ClientSecret"];
        
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException("Google OAuth configuration is missing");
        }
        
        // Build refresh token request
        var refreshRequest = new Dictionary<string, string>
        {
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["grant_type"] = "refresh_token"
        };
        
        try
        {
            // Make token refresh request to Google
            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(refreshRequest));
            
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Token refresh failed: {responseContent}");
            }
            
            // Parse refresh response
            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
            if (tokenResponse?.AccessToken == null)
            {
                throw new InvalidOperationException("Invalid refresh token response from Google");
            }
            
            // Calculate new expiration time
            var newExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
            
            // Encrypt new access token
            var encryptedAccessToken = _encryptionService.Encrypt(tokenResponse.AccessToken);
            
            // Update token entity
            tokenEntity.AccessToken = encryptedAccessToken;
            tokenEntity.ExpiresAt = newExpiresAt;
            tokenEntity.UpdatedAt = DateTime.UtcNow;
            
            // If Google provides a new refresh token, update it (rare but possible)
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                tokenEntity.RefreshToken = _encryptionService.Encrypt(tokenResponse.RefreshToken);
            }
            
            // Save changes to database
            _context.OAuthTokens.Update(tokenEntity);
            await _context.SaveChangesAsync();
            
            return tokenEntity;
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to refresh access token", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Invalid token refresh response", ex);
        }
    }

    public async Task RevokeTokensAsync(Guid userId)
    {
        // Find user's OAuth token
        var tokenEntity = await _context.OAuthTokens
            .FirstOrDefaultAsync(t => t.UserId == userId);
        
        if (tokenEntity == null)
        {
            return; // No tokens to revoke
        }
        
        try
        {
            // Decrypt refresh token for revocation
            var refreshToken = _encryptionService.Decrypt(tokenEntity.RefreshToken);
            
            // Revoke the token with Google
            var revokeRequest = new Dictionary<string, string>
            {
                ["token"] = refreshToken
            };
            
            var response = await _httpClient.PostAsync(
                "https://oauth2.googleapis.com/revoke",
                new FormUrlEncodedContent(revokeRequest));
            
            // Note: Google returns 200 even if token was already invalid
            // So we don't throw on non-success status codes here
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the operation
            // The important thing is to remove tokens from our database
            Console.WriteLine($"Warning: Failed to revoke token with Google: {ex.Message}");
        }
        
        // Remove token from our database regardless of Google revocation result
        _context.OAuthTokens.Remove(tokenEntity);
        await _context.SaveChangesAsync();
    }

    public void StoreCodeVerifier(string state, string codeVerifier)
    {
        // TODO: Store PKCE code verifier temporarily
        // 1. Add to in-memory dictionary with expiration
        // 2. In production, use Redis with TTL (5-10 minutes)
        
        _codeVerifiers[state] = codeVerifier;
    }

    public string? RetrieveCodeVerifier(string state)
    {
        // TODO: Retrieve and remove PKCE code verifier
        // 1. Try to get verifier from storage
        // 2. Remove it after retrieval (one-time use)
        // 3. Return null if not found
        
        _codeVerifiers.TryRemove(state, out var verifier);
        return verifier;
    }

    // HELPER METHODS TO IMPLEMENT:

    private string GenerateCodeVerifier()
    {
        // Generate 32 random bytes for PKCE code verifier
        var randomBytes = new byte[32];
        RandomNumberGenerator.Fill(randomBytes);
        
        // Base64URL encode (RFC 7636 requires URL-safe encoding)
        return Base64UrlEncode(randomBytes);
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        // SHA256 hash the code verifier
        var verifierBytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hashBytes = SHA256.HashData(verifierBytes);
        
        // Base64URL encode the hash
        return Base64UrlEncode(hashBytes);
    }

    private string Base64UrlEncode(byte[] data)
    {
        // Base64URL encoding per RFC 4648
        var base64 = Convert.ToBase64String(data);
        
        // Replace URL-unsafe characters
        return base64
            .Replace('+', '-')  // Replace + with -
            .Replace('/', '_')  // Replace / with _
            .TrimEnd('=');      // Remove padding
    }
}

// NOTES:
// - Always use PKCE (RFC 7636) for security
// - Store code verifiers temporarily (5-10 min expiration)
// - Handle HTTP errors and invalid responses
// - Never log tokens or sensitive data
// - Consider rate limiting token refresh attempts
// - Use HTTPS for all OAuth communications