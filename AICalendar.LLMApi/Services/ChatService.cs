using Microsoft.Extensions.AI;
using AICalendar.LLMApi.Models;
using System.Text.Json;

namespace AICalendar.LLMApi.Services
{
    public class ChatService : IChatService
    {
        private readonly ILogger<ChatService> _logger;
        private readonly IChatClient _chatClient;

        public ChatService(ILogger<ChatService> logger, IChatClient chatClient)
        {
            _logger = logger;
            _chatClient = chatClient;
        }

        public async Task<string> ProcessMessageAsync(string message)
        {
            _logger.LogInformation("Processing message in ChatService: {Message}", message);

            // Determine user intention using LLM
            var userIntention = await DetermineUserIntentionAsync(message);

            _logger.LogInformation("Determined user intention: {Intent} with confidence: {Confidence}",
                userIntention.Intent, userIntention.Confidence);

            // For now, return the LLM-generated response
            var response = userIntention.Response;

            _logger.LogInformation("Generated response: {Response}", response);

            return response;
        }

        public async Task<UserIntention> DetermineUserIntentionAsync(string message)
        {
            try
            {
                _logger.LogInformation("Analyzing user message for intent: {Message}", message);

                var systemPrompt = """
                You are an AI assistant for a calendar application. Your job is to analyze user messages and determine their intention. 
                
                Possible intents include:
                - CREATE_EVENT: User wants to create a new calendar event
                - UPDATE_EVENT: User wants to modify an existing event
                - DELETE_EVENT: User wants to remove an event
                - VIEW_EVENTS: User wants to see their calendar or events
                - SEARCH_EVENTS: User wants to find specific events
                - SET_REMINDER: User wants to set a reminder
                - GENERAL_QUESTION: User has a general question about the calendar
                - GREETING: User is greeting or making small talk
                - OTHER: Intent doesn't match any of the above categories

                Analyze the user's message and respond with a JSON object containing:
                {
                  "intent": "one of the intents listed above",
                  "confidence": "high/medium/low",
                  "entities": {
                    "extracted_entities": "any dates, times, event names, locations, etc."
                  },
                  "response": "a helpful response to the user based on their intent"
                }

                Be concise and helpful in your response.
                """;

                var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, message)
                };

                var response = await _chatClient.GetResponseAsync(chatMessages);
                var responseContent = response.Text.ToString() ?? "";

                _logger.LogInformation("LLM Response: {Response}", responseContent);

                // Parse the JSON response
                var userIntention = JsonSerializer.Deserialize<UserIntention>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return userIntention ?? new UserIntention
                {
                    Intent = "OTHER",
                    Confidence = "low",
                    Entities = new Dictionary<string, object>(),
                    Response = "I'm sorry, I couldn't understand your request. Could you please rephrase it?"
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse LLM response as JSON");
                return new UserIntention
                {
                    Intent = "OTHER",
                    Confidence = "low",
                    Entities = new Dictionary<string, object>(),
                    Response = "I'm having trouble understanding your request right now. Please try again."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining user intention");
                return new UserIntention
                {
                    Intent = "OTHER",
                    Confidence = "low",
                    Entities = new Dictionary<string, object>(),
                    Response = "I'm experiencing some technical difficulties. Please try again later."
                };
            }
        }
    }
}