namespace AICalendar.LLMApi.Services
{
    public class ChatService : IChatService
    {
        private readonly ILogger<ChatService> _logger;

        public ChatService(ILogger<ChatService> logger)
        {
            _logger = logger;
        }

        public async Task<string> ProcessMessageAsync(string message)
        {
            _logger.LogInformation("Processing message in ChatService: {Message}", message);

            // Mock processing time to simulate some work
            await Task.Delay(100);

            // Mock response - same as the original controller behavior
            var response = $"Got your message: \"{message}\". I'll get back to you shortly.";

            _logger.LogInformation("Generated response: {Response}", response);

            return response;
        }
    }
}