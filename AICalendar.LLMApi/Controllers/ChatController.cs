using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.LLMApi.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        [HttpPost("sendMessage")]
        public IActionResult SendMessage([FromBody] UserMessageDto userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage?.Message))
            {
                return BadRequest(new { error = "Message cannot be empty." });
            }

            // Mock response
            var response = new ChatResponseDto
            {
                Response = "Got your message: \"" + userMessage.Message + "\". I'll get back to you shortly."
            };

            return Ok(response);
        }
    }

    public class UserMessageDto
    {
        public string Message { get; set; }
    }

    public class ChatResponseDto
    {
        public string Response { get; set; }
    }
}
