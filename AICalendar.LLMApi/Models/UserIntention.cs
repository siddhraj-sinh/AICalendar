using ModelContextProtocol.Client;

namespace AICalendar.LLMApi.Models
{
    public class UserIntention
    {
        public string Intent { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public Dictionary<string, object> Entities { get; set; } = new();
        public string Response { get; set; } = string.Empty;
        public string McpToolToCall { get; set; } = string.Empty;

        public McpClientTool? McpClientTool { get; set; } = null;

    }
}