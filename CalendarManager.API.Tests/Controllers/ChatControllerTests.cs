using CalendarManager.API.Controllers;
using CalendarManager.API.Models.DTOs;
using CalendarManager.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CalendarManager.API.Tests.Controllers;

public class ChatControllerTests
{
    private readonly Mock<IClaudeService> _mockClaudeService;
    private readonly Mock<ILogger<ChatController>> _mockLogger;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        _mockClaudeService = new Mock<IClaudeService>();
        _mockLogger = new Mock<ILogger<ChatController>>();
        _controller = new ChatController(_mockClaudeService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Message_Returns200_WithChatResponse()
    {
        var request = new ChatMessageRequest
        {
            Message = "Hello, how are you?",
            UserEmail = "test@example.com"
        };

        var expectedResponse = new ChatResponse
        {
            Message = "I'm doing well, thank you!",
            Success = true,
            Type = MessageType.Info
        };

        _mockClaudeService.Setup(s => s.ProcessMessageAsync(request.Message, request.UserEmail, null))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.ProcessMessage(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task Message_Returns400_WhenMessageEmpty()
    {
        var request = new ChatMessageRequest
        {
            Message = "",
            UserEmail = "test@example.com"
        };

        var result = await _controller.ProcessMessage(request);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeOfType<ChatResponse>();
        var response = (ChatResponse)badRequest.Value!;
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task Message_Returns400_WhenMessageWhitespace()
    {
        var request = new ChatMessageRequest
        {
            Message = "   ",
            UserEmail = "test@example.com"
        };

        var result = await _controller.ProcessMessage(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Message_ReturnsActions_WhenToolsUsed()
    {
        var request = new ChatMessageRequest
        {
            Message = "Create a meeting for tomorrow at 2pm",
            UserEmail = "test@example.com"
        };

        var expectedResponse = new ChatResponse
        {
            Message = "I've created your meeting for tomorrow at 2pm.",
            Success = true,
            Type = MessageType.Success,
            Actions = new List<CalendarAction>
            {
                new CalendarAction
                {
                    Type = "create_event",
                    Executed = true,
                    Result = "Event created successfully"
                }
            }
        };

        _mockClaudeService.Setup(s => s.ProcessMessageAsync(request.Message, request.UserEmail, null))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.ProcessMessage(request);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ChatResponse>().Subject;
        response.Actions.Should().NotBeNullOrEmpty();
        response.Actions![0].Type.Should().Be("create_event");
        response.Actions[0].Executed.Should().BeTrue();
    }

    [Fact]
    public async Task Message_Returns500_WhenClaudeServiceThrows()
    {
        var request = new ChatMessageRequest
        {
            Message = "Hello",
            UserEmail = "test@example.com"
        };

        _mockClaudeService.Setup(s => s.ProcessMessageAsync(request.Message, request.UserEmail, null))
            .ThrowsAsync(new Exception("Service unavailable"));

        var result = await _controller.ProcessMessage(request);

        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        var response = statusResult.Value.Should().BeOfType<ChatResponse>().Subject;
        response.Success.Should().BeFalse();
        response.ErrorMessage.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task Message_UsesDefaultUserEmail_WhenNotProvided()
    {
        var request = new ChatMessageRequest
        {
            Message = "Hello"
        };

        var expectedResponse = new ChatResponse
        {
            Message = "Hello!",
            Success = true
        };

        _mockClaudeService.Setup(s => s.ProcessMessageAsync(request.Message, "test@example.com", null))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.ProcessMessage(request);

        _mockClaudeService.Verify(s => s.ProcessMessageAsync(request.Message, "test@example.com", null), Times.Once);
    }

    [Fact]
    public async Task Message_PassesConversationId_WhenProvided()
    {
        var conversationId = Guid.NewGuid().ToString();
        var request = new ChatMessageRequest
        {
            Message = "Continue conversation",
            UserEmail = "test@example.com",
            ConversationId = conversationId
        };

        var expectedResponse = new ChatResponse
        {
            Message = "Continuing...",
            Success = true,
            ConversationId = conversationId
        };

        _mockClaudeService.Setup(s => s.ProcessMessageAsync(request.Message, request.UserEmail, conversationId))
            .ReturnsAsync(expectedResponse);

        var result = await _controller.ProcessMessage(request);

        _mockClaudeService.Verify(s => s.ProcessMessageAsync(request.Message, request.UserEmail, conversationId), Times.Once);
    }

    [Fact]
    public void Health_Returns200_WithStatus()
    {
        var result = _controller.Health();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var status = value?.GetType().GetProperty("status")?.GetValue(value)?.ToString();
        var service = value?.GetType().GetProperty("service")?.GetValue(value)?.ToString();
        
        status.Should().Be("healthy");
        service.Should().Be("chat");
    }

    [Fact]
    public async Task GetConversation_Returns200_WithPlaceholderResponse()
    {
        var conversationId = Guid.NewGuid().ToString();

        var result = await _controller.GetConversation(conversationId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value;
        value.Should().NotBeNull();
    }
}
