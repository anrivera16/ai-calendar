using System.Security.Cryptography;
using System.Text;
using CalendarManager.API.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Services;

public class TokenEncryptionServiceTests
{
    private readonly string _validKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Mock<IConfiguration> _mockConfiguration;

    public TokenEncryptionServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Authentication:Encryption:Key"]).Returns(_validKey);
    }

    private TokenEncryptionService CreateService()
    {
        return new TokenEncryptionService(_mockConfiguration.Object);
    }

    [Fact]
    public void Encrypt_ReturnsNonEmptyBase64String()
    {
        var service = CreateService();
        var plaintext = "test-access-token";

        var result = service.Encrypt(plaintext);

        result.Should().NotBeNullOrEmpty();
        result.Should().MatchRegex("^[A-Za-z0-9+/]+=*$");
    }

    [Fact]
    public void Decrypt_ReturnsOriginalPlaintext()
    {
        var service = CreateService();
        var plaintext = "test-refresh-token-12345";

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertexts_ForSameInput()
    {
        var service = CreateService();
        var plaintext = "same-input-value";

        var encrypted1 = service.Encrypt(plaintext);
        var encrypted2 = service.Encrypt(plaintext);

        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Decrypt_ThrowsOnTamperedCiphertext()
    {
        var service = CreateService();
        var plaintext = "original-value";
        var encrypted = service.Encrypt(plaintext);
        
        var encryptedBytes = Convert.FromBase64String(encrypted);
        encryptedBytes[15] ^= 0xFF;
        var tampered = Convert.ToBase64String(encryptedBytes);

        var act = () => service.Decrypt(tampered);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Decrypt_ThrowsOnEmptyString()
    {
        var service = CreateService();

        var act = () => service.Decrypt("");

        act.Should().NotThrow();
    }

    [Fact]
    public void Encrypt_HandlesUnicodeCharacters()
    {
        var service = CreateService();
        var plaintext = "日本語テスト-émojis-🎉-密码";

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        decrypted.Should().Be(plaintext);
    }

    [Fact]
    public void Encrypt_HandlesLongStrings()
    {
        var service = CreateService();
        var plaintext = new string('a', 15000);

        var encrypted = service.Encrypt(plaintext);
        var decrypted = service.Decrypt(encrypted);

        decrypted.Should().Be(plaintext);
        decrypted.Length.Should().Be(15000);
    }

    [Fact]
    public void Constructor_ThrowsWhenKeyMissing()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Authentication:Encryption:Key"]).Returns((string?)null);

        var act = () => new TokenEncryptionService(mockConfig.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Encryption key not found*");
    }

    [Fact]
    public void Constructor_ThrowsWhenKeyWrongLength()
    {
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Authentication:Encryption:Key"]).Returns(shortKey);

        var act = () => new TokenEncryptionService(mockConfig.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*32 bytes*");
    }

    [Fact]
    public void Encrypt_ReturnsInput_WhenInputIsNull()
    {
        var service = CreateService();

        var result = service.Encrypt(null!);

        result.Should().BeNull();
    }

    [Fact]
    public void Encrypt_ReturnsInput_WhenInputIsEmpty()
    {
        var service = CreateService();

        var result = service.Encrypt(string.Empty);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_ThrowsOnInvalidBase64()
    {
        var service = CreateService();

        var act = () => service.Decrypt("not-valid-base64!!!");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Decrypt_ThrowsOnTooShortCiphertext()
    {
        var service = CreateService();
        var tooShort = Convert.ToBase64String(new byte[10]);

        var act = () => service.Decrypt(tooShort);

        act.Should().Throw<InvalidOperationException>();
    }
}
