using CalendarManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ITokenEncryptionService _encryptionService;

    public TestController(ITokenEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    [HttpPost("encrypt")]
    public IActionResult TestEncryption([FromBody] TestEncryptionRequest request)
    {
        try
        {
            var encrypted = _encryptionService.Encrypt(request.Text);
            var decrypted = _encryptionService.Decrypt(encrypted);

            return Ok(new
            {
                Original = request.Text,
                Encrypted = encrypted,
                Decrypted = decrypted,
                Success = decrypted == request.Text
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}

public record TestEncryptionRequest(string Text);