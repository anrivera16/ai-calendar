using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models;

namespace CalendarManager.API.Services.Interfaces;

public interface IOAuthService
{
    /// <summary>
    /// Generates Google OAuth2 authorization URL with PKCE
    /// </summary>
    /// <param name="state">Random state parameter for CSRF protection</param>
    /// <returns>Authorization URL to redirect user to</returns>
    string GetAuthorizationUrl(string state);
    
    /// <summary>
    /// Exchanges authorization code for access/refresh tokens
    /// </summary>
    /// <param name="code">Authorization code from Google</param>
    /// <param name="state">State parameter for verification</param>
    /// <param name="userId">User ID to associate tokens with</param>
    /// <returns>OAuth token entity (encrypted)</returns>
    Task<OAuthToken> ExchangeCodeForTokensAsync(string code, string state, Guid userId);
    
    /// <summary>
    /// Gets a valid access token, refreshing if necessary
    /// </summary>
    /// <param name="userId">User ID to get token for</param>
    /// <returns>Valid decrypted access token</returns>
    Task<string> GetValidAccessTokenAsync(Guid userId);
    
    /// <summary>
    /// Refreshes an expired access token using refresh token
    /// </summary>
    /// <param name="tokenEntity">Token entity to refresh</param>
    /// <returns>Updated token entity</returns>
    Task<OAuthToken> RefreshAccessTokenAsync(OAuthToken tokenEntity);
    
    /// <summary>
    /// Revokes tokens and removes from database
    /// </summary>
    /// <param name="userId">User ID to revoke tokens for</param>
    Task RevokeTokensAsync(Guid userId);
    
    /// <summary>
    /// Stores PKCE code verifier temporarily for OAuth flow
    /// </summary>
    /// <param name="state">State parameter</param>
    /// <param name="codeVerifier">PKCE code verifier</param>
    void StoreCodeVerifier(string state, string codeVerifier);
    
    /// <summary>
    /// Retrieves and removes PKCE code verifier
    /// </summary>
    /// <param name="state">State parameter</param>
    /// <returns>PKCE code verifier</returns>
    string? RetrieveCodeVerifier(string state);
    
    /// <summary>
    /// Fetches user profile information from Google's userinfo endpoint
    /// </summary>
    /// <param name="accessToken">Valid access token from Google</param>
    /// <returns>User profile information from Google</returns>
    Task<GoogleUserInfo> GetUserInfoAsync(string accessToken);
}