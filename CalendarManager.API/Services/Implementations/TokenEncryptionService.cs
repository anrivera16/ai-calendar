using System.Security.Cryptography;
using System.Text;
using CalendarManager.API.Services.Interfaces;

namespace CalendarManager.API.Services.Implementations;

public class TokenEncryptionService : ITokenEncryptionService
{
    private readonly byte[] _encryptionKey;

    public TokenEncryptionService(IConfiguration configuration)
    {
        // TODO: Get encryption key from configuration
        // 1. Read "Authentication:Encryption:Key" from appsettings.json
        // 2. Convert from Base64 to byte array
        // 3. Validate key is exactly 32 bytes (256 bits)
        // 4. Throw InvalidOperationException if key is missing/invalid
        
        var keyBase64 = configuration["Authentication:Encryption:Key"];
        if (string.IsNullOrEmpty(keyBase64))
        {
            throw new InvalidOperationException("Encryption key not found in configuration");
        }

        _encryptionKey = Convert.FromBase64String(keyBase64);
        
        if (_encryptionKey.Length != 32)
        {
            throw new InvalidOperationException("Encryption key must be 32 bytes (256 bits)");
        }
    }

    public string Encrypt(string plaintext)
    {
        // Handle null/empty plaintext
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        try
        {
            // Convert plaintext to bytes (UTF-8)
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            
            // Generate random IV (12 bytes for GCM)
            var iv = new byte[12];
            RandomNumberGenerator.Fill(iv);
            
            // Create ciphertext buffer
            var ciphertext = new byte[plaintextBytes.Length];
            
            // Create auth tag buffer (16 bytes for GCM)
            var authTag = new byte[16];
            
            // Perform AES-256-GCM encryption
            using (var aes = new AesGcm(_encryptionKey, 16)) // 16-byte tag size
            {
                aes.Encrypt(iv, plaintextBytes, ciphertext, authTag);
            }
            
            // Combine: IV (12) + ciphertext (variable) + auth tag (16)
            var combined = new byte[iv.Length + ciphertext.Length + authTag.Length];
            Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, combined, iv.Length, ciphertext.Length);
            Buffer.BlockCopy(authTag, 0, combined, iv.Length + ciphertext.Length, authTag.Length);
            
            // Return Base64 encoded result
            return Convert.ToBase64String(combined);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            throw new InvalidOperationException("Failed to encrypt data", ex);
        }
    }

    public string Decrypt(string ciphertext)
    {
        // Handle null/empty ciphertext
        if (string.IsNullOrEmpty(ciphertext))
            return ciphertext;

        try
        {
            // Convert from Base64 to byte array
            var combined = Convert.FromBase64String(ciphertext);
            
            // Validate minimum length (IV + auth tag = 28 bytes minimum)
            if (combined.Length < 28)
                throw new ArgumentException("Invalid ciphertext length");
            
            // Extract components
            var iv = new byte[12];
            var authTag = new byte[16];
            var encryptedData = new byte[combined.Length - iv.Length - authTag.Length];
            
            Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(combined, iv.Length, encryptedData, 0, encryptedData.Length);
            Buffer.BlockCopy(combined, iv.Length + encryptedData.Length, authTag, 0, authTag.Length);
            
            // Create plaintext buffer
            var plaintextBytes = new byte[encryptedData.Length];
            
            // Perform AES-256-GCM decryption with authentication
            using (var aes = new AesGcm(_encryptionKey, 16)) // 16-byte tag size
            {
                aes.Decrypt(iv, encryptedData, authTag, plaintextBytes);
            }
            
            // Convert decrypted bytes back to string (UTF-8)
            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
        {
            throw new InvalidOperationException("Failed to decrypt data", ex);
        }
    }
}

// NOTES:
// - Use AES-GCM for authenticated encryption (prevents tampering)
// - Always use cryptographically secure random IVs
// - Never reuse IVs with the same key
// - Handle CryptographicException for invalid data/keys
// - Consider using Span<byte> for better performance
// - IV size for GCM is typically 12 bytes (96 bits)
// - Auth tag size for GCM is typically 16 bytes (128 bits)