using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AICalendar.LLMApi.Services;
using AICalendar.LLMApi.DTOs;

namespace AICalendar.LLMApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatService chatService, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        [HttpPost("sendMessage")]
        public async Task<IActionResult> SendMessage([FromBody] UserMessageDto userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage?.Message))
            {
                return BadRequest(new { error = "Message cannot be empty." });
            }

            try
            {
                _logger.LogInformation("Received message: {Message}", userMessage.Message);

                // Use the chat service to process the message
                var responseMessage = await _chatService.ProcessMessageAsync(userMessage.Message);

                var response = new ChatResponseDto
                {
                    Response = responseMessage
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message}", userMessage.Message);
                return StatusCode(500, new { error = "An error occurred while processing your message." });
            }
        }
    }
}
