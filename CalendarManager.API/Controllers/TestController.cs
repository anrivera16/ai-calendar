using CalendarManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly ITokenEncryptionService _encryptionService;
    private readonly IHostEnvironment _environment;

    public TestController(ITokenEncryptionService encryptionService, IHostEnvironment environment)
    {
        _encryptionService = encryptionService;
        _environment = environment;
    }

    [HttpPost("encrypt")]
    public IActionResult TestEncryption([FromBody] TestEncryptionRequest request)
    {
        // Security: Only allow this endpoint in development environment
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

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