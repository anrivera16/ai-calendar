using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<ChatController> _logger;
    
    // Maximum message length to prevent DoS
    private const int MaxMessageLength = 10000;
    private const int MaxConversationIdLength = 100;

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

        // Input validation: Limit message length to prevent DoS
        if (request.Message.Length > MaxMessageLength)
        {
            return BadRequest(new ChatResponse 
            { 
                Success = false, 
                ErrorMessage = "Message is too long",
                Type = MessageType.Error,
                Message = $"Message must be less than {MaxMessageLength} characters."
            });
        }

        // Validate conversation ID format if provided
        if (!string.IsNullOrEmpty(request.ConversationId))
        {
            if (request.ConversationId.Length > MaxConversationIdLength)
            {
                return BadRequest(new ChatResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Invalid conversation ID",
                    Type = MessageType.Error,
                    Message = "Invalid conversation ID format."
                });
            }
        }

        try
        {
            // Use provided user email or default for demo
            var userId = request.UserEmail ?? "test@example.com";
            
            // Validate email format if provided
            if (!string.IsNullOrEmpty(request.UserEmail) && !IsValidEmail(request.UserEmail))
            {
                return BadRequest(new ChatResponse 
                { 
                    Success = false, 
                    ErrorMessage = "Invalid email format",
                    Type = MessageType.Error,
                    Message = "Please provide a valid email address."
                });
            }
            
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
            _logger.LogError(ex, "Error processing chat message");
            
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
        // Validate conversation ID format
        if (string.IsNullOrEmpty(conversationId) || 
            conversationId.Length > MaxConversationIdLength ||
            !Regex.IsMatch(conversationId, @"^[a-zA-Z0-9\-]+$"))
        {
            return BadRequest(new { error = "Invalid conversation ID" });
        }
        
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

    /// <summary>
    /// Validates email format using simple regex pattern
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, emailPattern);
    }
}