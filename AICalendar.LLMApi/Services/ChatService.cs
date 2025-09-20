using AICalendar.Chat.Services;
using AICalendar.LLMApi.Models;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Text.Json;

namespace AICalendar.LLMApi.Services
{
    public class ChatService : IChatService
    {
        private readonly ILogger<ChatService> _logger;
        private readonly IChatClient _chatClient;
        private readonly McpService _mcpService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ChatService(ILogger<ChatService> logger, IChatClient chatClient, McpService mcpService, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _chatClient = chatClient;
            _mcpService = mcpService;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> ProcessMessageAsync(string message)
        {
            _logger.LogInformation("[ProcessMessageAsync] Start processing message: {Message}", message);
                   
            var accessToken = GetAccessToken();
            _logger.LogDebug("[ProcessMessageAsync] Retrieved access token: {AccessToken}", accessToken);

            var userIntention = await DetermineUserIntentionAsync(message);

            _logger.LogInformation("[ProcessMessageAsync] Determined user intention: {Intent} with confidence: {Confidence}",
                userIntention.Intent, userIntention.Confidence);

            // If LLM suggested an MCP tool to call, execute it
            if (!string.IsNullOrEmpty(userIntention.McpToolToCall))
            {
                try
                {
                    _logger.LogInformation("[ProcessMessageAsync] MCP tool suggested: {ToolName}", userIntention.McpToolToCall);

                    var arguments = await ExtractToolArgumentsAsync(userIntention, message);
                    _logger.LogDebug("[ProcessMessageAsync] Extracted arguments for MCP tool: {Arguments}", arguments);

                    var mcpResult = await ExecuteMcpToolAsync(userIntention, message, arguments);
                    _logger.LogInformation("[ProcessMessageAsync] MCP tool execution result: {Result}", mcpResult);

                    var draftedResponse = await DraftUserResponseAsync(mcpResult, message);
                    _logger.LogInformation("[ProcessMessageAsync] Drafted user response: {Response}", draftedResponse);

                    return draftedResponse;

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ProcessMessageAsync] Failed to execute MCP tool: {ToolName}", userIntention.McpToolToCall);
                    return $"I encountered an error while processing your request. {userIntention.Response}";
                }
            }

            // For now, return the LLM-generated response
            var response = userIntention.Response;

            _logger.LogInformation("[ProcessMessageAsync] Generated response: {Response}", response);

            return response;
        }

        private async Task<UserIntention> DetermineUserIntentionAsync(string message)
        {
            _logger.LogInformation("[DetermineUserIntentionAsync] Start analyzing user message: {Message}", message);

            try
            {
                // Get available MCP tools
                var client = await _mcpService.GetClientAsync();
                _logger.LogDebug("[DetermineUserIntentionAsync] Retrieved MCP client.");

                var tools = await client.ListToolsAsync();
                _logger.LogDebug("[DetermineUserIntentionAsync] Retrieved list of MCP tools: {Tools}", tools);

                // Build the tools description for the prompt
                var toolsDescription = string.Join("\n", tools.Select(tool =>
                    $"- {tool.Name}: {tool.Description}"));

                var systemPrompt = $$"""
You are an AI assistant for a calendar application. Your job is to analyze user messages and determine their intention.

Available MCP tools:
{toolsDescription}

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

CRITICAL INSTRUCTIONS for mcpToolToCall field:
- You MUST use the EXACT tool name as provided in the Available MCP tools list above
- DO NOT modify, change case, add spaces, or alter the tool name in ANY way
- Copy the tool name EXACTLY as it appears (e.g., "create_calendar_event", "get_calendar_events")
- If no MCP tool is needed for the user's request, use an empty string ""

Examples of correct tool mapping:
- If user wants to create an event → use "create_calendar_event" (exact name from tools list)
- If user wants to view events → use "get_calendar_events" (exact name from tools list)
- If user is just greeting → use "" (empty string)

Analyze the user's message and respond with a JSON object containing:
{
  "intent": "one of the intents listed above",
  "confidence": "high/medium/low",
  "entities": {
    "extracted_entities": "any dates, times, event names, locations, etc."
  },
  "response": "a helpful response to the user based on their intent",
  "mcpToolToCall": "EXACT tool name from the Available MCP tools list above, or empty string if no tool needed"
}

Rules for selecting MCP tools:
- For VIEW_EVENTS or SEARCH_EVENTS intents, use the appropriate calendar retrieval tool
- For CREATE_EVENT, UPDATE_EVENT, DELETE_EVENT intents, use the corresponding MCP tool if available
- For GREETING, GENERAL_QUESTION, or OTHER intents, set mcpToolToCall to empty string
- Only specify tools that are actually available in the MCP tools list above

Be concise and helpful in your response.
Remember: The mcpToolToCall field must contain the EXACT tool name as listed in the Available MCP tools section. Do not interpret or modify these names.
""";

                var chatMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, message)
        };

                var response = await _chatClient.GetResponseAsync(chatMessages);
                var responseContent = response.Text.ToString().Replace("json", "", StringComparison.OrdinalIgnoreCase)
                                                            .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                                                    .Replace("```JSON", "", StringComparison.OrdinalIgnoreCase) ?? "";

                _logger.LogInformation("[DetermineUserIntentionAsync] LLM Response: {Response}", responseContent);

                // Parse the JSON response
                var userIntention = JsonSerializer.Deserialize<UserIntention>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Validate that the suggested MCP tool actually exists
                if (userIntention != null && !string.IsNullOrEmpty(userIntention.McpToolToCall))
                {
                    var toolExists = tools.Any(t => t.Name.Equals(userIntention.McpToolToCall, StringComparison.OrdinalIgnoreCase));
                    if (!toolExists)
                    {
                        _logger.LogWarning("[DetermineUserIntentionAsync] LLM suggested non-existent MCP tool: {ToolName}. Setting to empty.", userIntention.McpToolToCall);
                        userIntention.McpToolToCall = string.Empty;
                    }
                    userIntention.McpClientTool = tools.FirstOrDefault(t => t.Name.Equals(userIntention.McpToolToCall, StringComparison.OrdinalIgnoreCase));
                }

                _logger.LogInformation("[DetermineUserIntentionAsync] Completed analysis. Intent: {Intent}, Confidence: {Confidence}", userIntention?.Intent, userIntention?.Confidence);

                return userIntention ?? new UserIntention
                {
                    Intent = "OTHER",
                    Confidence = "low",
                    Entities = new Dictionary<string, object>(),
                    Response = "I'm sorry, I couldn't understand your request. Could you please rephrase it?",
                    McpToolToCall = string.Empty
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[DetermineUserIntentionAsync] Failed to parse LLM response as JSON");
                return new UserIntention
                {
                    Intent = "OTHER",
                    Confidence = "low",
                    Entities = new Dictionary<string, object>(),
                    Response = "I'm having trouble understanding your request right now. Please try again.",
                    McpToolToCall = string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DetermineUserIntentionAsync] Error determining user intention");
                return new UserIntention
                {
                    Intent = "OTHER",
                    Confidence = "low",
                    Entities = new Dictionary<string, object>(),
                    Response = "I'm experiencing some technical difficulties. Please try again later.",
                    McpToolToCall = string.Empty
                };
            }
        }

        private async Task<string> ExecuteMcpToolAsync(UserIntention userIntention, string originalMessage, Dictionary<string, object> arguments)
        {
            _logger.LogInformation("[ExecuteMcpToolAsync] Start executing MCP tool: {ToolName}", userIntention.McpClientTool?.Name);

            try
            {
                var client = await _mcpService.GetClientAsync();
                _logger.LogDebug("[ExecuteMcpToolAsync] Retrieved MCP client.");

                var accessToken = GetAccessToken();
                arguments.Add("accessToken", accessToken);
                _logger.LogDebug("[ExecuteMcpToolAsync] Added access token to arguments.");

                var response = await client.CallToolAsync(userIntention.McpClientTool.Name, arguments);

                if (response.Content?.Any() == true)
                {
                    // Extract text content from the response
                    var textContentBlock = response.Content.FirstOrDefault(content => content.Type == "text");
                    if (textContentBlock is TextContentBlock textContent)
                    {
                        _logger.LogInformation("[ExecuteMcpToolAsync] Successfully executed MCP tool. Result: {Result}", textContent.Text);
                        return textContent.Text ?? "No content returned from the tool.";
                    }
                }

                _logger.LogWarning("[ExecuteMcpToolAsync] No response content received from the tool.");
                return "No response received from the tool.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExecuteMcpToolAsync] Error executing MCP tool: {ToolName}", userIntention.McpClientTool?.Name);
                return "I encountered an error while processing your request.";
            }
        }

        private async Task<Dictionary<string, object>> ExtractToolArgumentsAsync(UserIntention userIntention, string message)
        {
            _logger.LogInformation("[ExtractToolArgumentsAsync] Start extracting arguments for tool: {ToolName}", userIntention.McpClientTool?.Name);

            if (userIntention.McpClientTool == null)
            {
                _logger.LogWarning("[ExtractToolArgumentsAsync] No MCP tool provided. Returning empty arguments.");
                return new Dictionary<string, object>();
            }

            try
            {
                // Get current date/time info
                var now = DateTime.Now;
                var currentDate = now.Date;
                var currentTime = now.ToString("HH:mm:ss");

                // Get the tool's input schema
                var toolSchema = JsonSerializer.Serialize(userIntention.McpClientTool.JsonSchema, new JsonSerializerOptions { WriteIndented = true });

                var systemPrompt = $$"""
You are an expert at extracting structured data from user messages based on JSON schemas.

CURRENT DATE AND TIME INFORMATION:
- Current Date: {{now:yyyy-MM-dd}} ({{now:dddd, MMMM dd, yyyy}})
- Current Time: {{currentTime}}
- Current Year: {{now:yyyy}}

Tool Information:
- Tool Name: {{userIntention.McpClientTool.Name}}
- Tool Description: {{userIntention.McpClientTool.Description}}

Tool Input Schema (USE EXACT ARGUMENT NAMES FROM THIS SCHEMA):
{{toolSchema}}

CRITICAL INSTRUCTIONS:
1. You MUST use the EXACT argument names as they appear in the JSON schema above
2. DO NOT modify, change case, or alter argument names in ANY way
3. Copy argument names EXACTLY as defined in the schema properties
4. Pay attention to the exact spelling and casing of each property name

DATE/TIME EXTRACTION RULES:
- Use the current date/time information provided above as your reference
- For relative date references, calculate from the current date:
  • "today" = {{currentDate:yyyy-MM-dd}}
  • "tomorrow" = {{currentDate.AddDays(1):yyyy-MM-dd}}
  • "tonight" = {{currentDate:yyyy-MM-dd}} (evening time, suggest 20:00:00 if no specific time given)
  • "yesterday" = {{currentDate.AddDays(-1):yyyy-MM-dd}}
  • "this week" = current week range
  • "next week" = next week range
  • "this month" = {{currentDate:yyyy-MM}}-01 to {{DateTime.DaysInMonth(currentDate.Year, currentDate.Month):D2}}
  • "next month" = next month's range

TIME HANDLING:
- For times without dates: assume current date
- For "tonight": use current date with evening time (20:00:00 if not specified)
- For "morning": use 09:00:00
- For "afternoon": use 14:00:00
- For "evening": use 20:00:00
- For date/time values, use ISO 8601 format: YYYY-MM-DDTHH:mm:ss

DATA TYPE RULES:
- Use correct data types as specified in the schema (string, number, boolean, array, object)
- For date/time: use ISO 8601 format strings
- For numbers: use actual numeric values, not strings
- For booleans: use true/false, not "true"/"false"
- For required parameters that cannot be determined: set to null
- For optional parameters not mentioned: omit from response

ARGUMENT NAME VALIDATION:
- Before including any argument, verify it exists in the schema properties
- Use the EXACT case-sensitive name from the schema
- Do not add arguments that don't exist in the schema
- Do not rename or transform argument names

Examples of date calculations from current date ({{now:yyyy-MM-dd}}):
- "create an event tonight" → startDateTime: "{{currentDate:yyyy-MM-dd}}T20:00:00"
- "schedule for tomorrow" → startDateTime: "{{currentDate.AddDays(1):yyyy-MM-dd}}T09:00:00"
- "meeting at 3pm today" → startDateTime: "{{currentDate:yyyy-MM-dd}}T15:00:00"

Analyze the user message and extract arguments. Respond with a JSON object containing ONLY the extracted arguments using EXACT schema property names:

{
  "exact_property_name_from_schema": "value",
  "another_exact_property_name": "value"
}

If no arguments can be extracted, respond with: {}
""";

                var chatMessages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, $"User message: {message}")
        };

                var response = await _chatClient.GetResponseAsync(chatMessages);
                var responseContent = response.Text.ToString()
                    .Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("```JSON", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                    .Trim() ?? "";

                _logger.LogInformation("[ExtractToolArgumentsAsync] Tool arguments extraction response: {Response}", responseContent);

                // Parse the JSON response with exact property name matching
                var arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false // Use exact case matching
                });

                _logger.LogInformation("[ExtractToolArgumentsAsync] Extracted arguments: {Arguments}", arguments);

                // Validate that all extracted arguments exist in the schema
                if (arguments != null && arguments.Any())
                {
                    var schemaObj = JsonSerializer.Deserialize<JsonElement>(toolSchema);
                    var schemaProperties = new HashSet<string>();

                    if (schemaObj.TryGetProperty("properties", out var props))
                    {
                        foreach (var prop in props.EnumerateObject())
                        {
                            schemaProperties.Add(prop.Name);
                        }
                    }

                    // Remove any arguments that don't exist in schema
                    var validArguments = arguments.Where(kvp => schemaProperties.Contains(kvp.Key))
                                                 .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    if (validArguments.Count != arguments.Count)
                    {
                        var invalidArgs = arguments.Keys.Except(schemaProperties).ToList();
                        _logger.LogWarning("[ExtractToolArgumentsAsync] Removed invalid arguments not in schema: {InvalidArgs}. Valid schema properties: {ValidProps}",
                            string.Join(", ", invalidArgs), string.Join(", ", schemaProperties));
                    }

                    return validArguments;
                }

                return arguments ?? new Dictionary<string, object>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[ExtractToolArgumentsAsync] Failed to parse tool arguments extraction response");
                return new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExtractToolArgumentsAsync] Error extracting tool arguments");
                return new Dictionary<string, object>();
            }
        }
        private async Task<string> DraftUserResponseAsync(string mcpToolResult, string originalUserMessage)
        {
            _logger.LogInformation("[DraftUserResponseAsync] Start drafting user response.");

            try
            {
                var systemPrompt = """
            You are an AI assistant for a calendar application. Your job is to create a natural, helpful response to the user based on:
            1. The original user message
            2. The result from executing a calendar tool/operation

            Guidelines for crafting responses:
            - Be conversational and friendly
            - Acknowledge what the user requested
            - Present the tool result in a clear, user-friendly format
            - If the tool result contains structured data (like events), format it nicely
            - If there was an error, explain it in simple terms and suggest next steps
            - Keep responses concise but informative
            - Use natural language, avoid technical jargon
            - If the result is about calendar events, format dates and times in a readable way
            
            Examples of good responses:
            - For successful event creation: "Great! I've created your meeting for [date/time]. You're all set!"
            - For viewing events: "Here are your upcoming events: [formatted list]"
            - For no events found: "I don't see any events matching your criteria. Would you like to create one?"
            - For errors: "I had trouble with that request. [brief explanation of issue]"
            
            Always maintain a helpful and professional tone while being personable.
            """;

                var chatMessages = new List<ChatMessage>
                {
                    new ChatMessage(ChatRole.System, systemPrompt),
                    new ChatMessage(ChatRole.User, $"Original user message: {originalUserMessage}\n\nTool execution result: {mcpToolResult}")
                };

                var response = await _chatClient.GetResponseAsync(chatMessages);
                var responseContent = response.Text.ToString() ?? "I've processed your request.";

                _logger.LogInformation("[DraftUserResponseAsync] Drafted response: {Response}", responseContent);

                return responseContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DraftUserResponseAsync] Error drafting user response");
                return "I've completed your request, but I'm having trouble formatting the response right now.";
            }
        }

        private string? GetAccessToken()
        {
            _logger.LogInformation("[GetAccessToken] Retrieving access token from HTTP context.");

            var authHeader = _httpContextAccessor.HttpContext?
                .Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("[GetAccessToken] Authorization header is missing or invalid.");
                return null;
            }

            var accessToken = authHeader.Substring("Bearer ".Length).Trim();
            _logger.LogDebug("[GetAccessToken] Retrieved access token: {AccessToken}", accessToken);

            return accessToken;
        }
    }
}