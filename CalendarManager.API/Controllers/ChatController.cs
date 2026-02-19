using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IClaudeService claudeService, ILogger<ChatController> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
    }

    /// <summary>
    /// Process a chat message with AI assistant
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> ProcessMessage([FromBody] ChatMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new ChatResponse 
            { 
                Success = false, 
                ErrorMessage = "Message cannot be empty",
                Type = MessageType.Error,
                Message = "Please provide a message to process."
            });
        }

        try
        {
            // Use provided user email or default for demo
            var userId = request.UserEmail ?? "test@example.com";
            
            _logger.LogInformation("Processing chat message for user: {UserId}", userId);

            var response = await _claudeService.ProcessMessageAsync(
                request.Message, 
                userId, 
                request.ConversationId
            );

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message: {Message}", request.Message);
            
            return StatusCode(500, new ChatResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred",
                Type = MessageType.Error,
                Message = "Sorry, I encountered an error processing your request. Please try again."
            });
        }
    }

    /// <summary>
    /// Get conversation history (placeholder for future implementation)
    /// </summary>
    [HttpGet("conversation/{conversationId}")]
    public async Task<IActionResult> GetConversation(string conversationId)
    {
        // Placeholder for future conversation history implementation
        await Task.Delay(1); // Remove when implementing
        
        return Ok(new 
        { 
            conversationId,
            messages = new List<object>(),
            message = "Conversation history not yet implemented"
        });
    }

    /// <summary>
    /// Health check endpoint for the chat service
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "chat", timestamp = DateTime.UtcNow });
    }
}