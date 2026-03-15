using CalendarManager.API.Data;
using CalendarManager.API.Data.Entities;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CalendarManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IClaudeService _claudeService;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IClaudeService claudeService,
        AppDbContext dbContext,
        ILogger<ChatController> logger)
    {
        _claudeService = claudeService;
        _dbContext = dbContext;
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
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new ChatResponse 
                { 
                    Success = false, 
                    ErrorMessage = "User not authenticated",
                    Type = MessageType.Error,
                    Message = "Please log in to use the chat feature."
                });
            }
            
            // Parse user ID (could be email or Guid)
            Guid userDbId;
            if (Guid.TryParse(userIdClaim, out var parsedGuid))
            {
                userDbId = parsedGuid;
            }
            else
            {
                // Try to find user by email
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userIdClaim);
                if (user == null)
                {
                    return Unauthorized(new ChatResponse
                    {
                        Success = false,
                        ErrorMessage = "User not found",
                        Type = MessageType.Error,
                        Message = "User not found. Please authenticate again."
                    });
                }
                userDbId = user.Id;
            }
            
            _logger.LogInformation("Processing chat message for user: {UserId}", userIdClaim);

            // Handle conversation ID - create new or validate existing
            string? conversationId = request.ConversationId;
            if (string.IsNullOrEmpty(conversationId))
            {
                // Create new conversation
                var newConversation = new Conversation
                {
                    Id = Guid.NewGuid(),
                    UserId = userDbId,
                    Title = request.Message.Length > 100 ? request.Message[..100] + "..." : request.Message,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Conversations.Add(newConversation);
                await _dbContext.SaveChangesAsync();
                conversationId = newConversation.Id.ToString();
                _logger.LogInformation("Created new conversation: {ConversationId}", conversationId);
            }
            else
            {
                // Validate conversation exists and belongs to user
                if (!Guid.TryParse(conversationId, out var convGuid))
                {
                    return BadRequest(new ChatResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid conversation ID",
                        Type = MessageType.Error,
                        Message = "Invalid conversation ID format."
                    });
                }
                
                var conversation = await _dbContext.Conversations
                    .FirstOrDefaultAsync(c => c.Id == convGuid && c.UserId == userDbId);
                
                if (conversation == null)
                {
                    return NotFound(new ChatResponse
                    {
                        Success = false,
                        ErrorMessage = "Conversation not found",
                        Type = MessageType.Error,
                        Message = "Conversation not found or does not belong to you."
                    });
                }
                
                // Update conversation timestamp
                conversation.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            var response = await _claudeService.ProcessMessageAsync(
                request.Message, 
                userIdClaim, 
                conversationId
            );

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ChatResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                Type = MessageType.Error,
                Message = ex.Message
            });
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
    /// Get conversation history
    /// </summary>
    [HttpGet("conversation/{conversationId}")]
    public async Task<IActionResult> GetConversation(string conversationId)
    {
        try
        {
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }
            
            // Parse user ID
            Guid userDbId;
            if (Guid.TryParse(userIdClaim, out var parsedGuid))
            {
                userDbId = parsedGuid;
            }
            else
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userIdClaim);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }
                userDbId = user.Id;
            }
            
            // Parse conversation ID
            if (!Guid.TryParse(conversationId, out var convGuid))
            {
                return BadRequest(new { error = "Invalid conversation ID format" });
            }
            
            // Get conversation with messages
            var conversation = await _dbContext.Conversations
                .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(c => c.Id == convGuid && c.UserId == userDbId);
            
            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }
            
            return Ok(new
            {
                conversationId = conversation.Id.ToString(),
                title = conversation.Title,
                createdAt = conversation.CreatedAt,
                updatedAt = conversation.UpdatedAt,
                messages = conversation.Messages.Select(m => new
                {
                    id = m.Id.ToString(),
                    role = m.Role,
                    content = m.Content,
                    timestamp = m.CreatedAt
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to retrieve conversation" });
        }
    }

    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        try
        {
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }
            
            // Parse user ID
            Guid userDbId;
            if (Guid.TryParse(userIdClaim, out var parsedGuid))
            {
                userDbId = parsedGuid;
            }
            else
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userIdClaim);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }
                userDbId = user.Id;
            }
            
            // Get all conversations for user, ordered by most recent
            var conversations = await _dbContext.Conversations
                .Where(c => c.UserId == userDbId)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new
                {
                    conversationId = c.Id.ToString(),
                    title = c.Title,
                    createdAt = c.CreatedAt,
                    updatedAt = c.UpdatedAt,
                    messageCount = c.Messages.Count
                })
                .ToListAsync();
            
            return Ok(new { conversations });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return StatusCode(500, new { error = "Failed to retrieve conversations" });
        }
    }

    /// <summary>
    /// Delete a conversation
    /// </summary>
    [HttpDelete("conversation/{conversationId}")]
    public async Task<IActionResult> DeleteConversation(string conversationId)
    {
        try
        {
            // Get user ID from JWT claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }
            
            // Parse user ID
            Guid userDbId;
            if (Guid.TryParse(userIdClaim, out var parsedGuid))
            {
                userDbId = parsedGuid;
            }
            else
            {
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userIdClaim);
                if (user == null)
                {
                    return Unauthorized(new { error = "User not found" });
                }
                userDbId = user.Id;
            }
            
            // Parse conversation ID
            if (!Guid.TryParse(conversationId, out var convGuid))
            {
                return BadRequest(new { error = "Invalid conversation ID format" });
            }
            
            // Find conversation
            var conversation = await _dbContext.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == convGuid && c.UserId == userDbId);
            
            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }
            
            // Delete messages first (cascade should handle this but being explicit)
            _dbContext.Messages.RemoveRange(conversation.Messages);
            _dbContext.Conversations.Remove(conversation);
            await _dbContext.SaveChangesAsync();
            
            _logger.LogInformation("Deleted conversation {ConversationId}", conversationId);
            
            return Ok(new { success = true, message = "Conversation deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation {ConversationId}", conversationId);
            return StatusCode(500, new { error = "Failed to delete conversation" });
        }
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