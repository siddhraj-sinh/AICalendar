using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class LlmProxyController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;

    public LlmProxyController(IDownstreamApi downstreamApi)
    {
        _downstreamApi = downstreamApi;
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
            // Call the ChatController's SendMessage endpoint via DownstreamApi
            var response = await _downstreamApi.PostForUserAsync<UserMessageDto, ChatResponseDto>("LlmApi", userMessage, options =>
            {
                options.RelativePath = "/api/Chat/sendMessage";
            });

            return Ok(new { response.Response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to process the message.", details = ex.Message });
        }
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
