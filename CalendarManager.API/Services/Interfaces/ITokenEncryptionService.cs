namespace CalendarManager.API.Services.Interfaces;

public interface ITokenEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string using AES-256-GCM
    /// </summary>
    /// <param name="plaintext">The text to encrypt (OAuth token)</param>
    /// <returns>Base64 encoded encrypted data (IV + ciphertext + auth tag)</returns>
    string Encrypt(string plaintext);
    
    /// <summary>
    /// Decrypts an encrypted string back to plaintext
    /// </summary>
    /// <param name="ciphertext">Base64 encoded encrypted data</param>
    /// <returns>Original plaintext string</returns>
    string Decrypt(string ciphertext);
}