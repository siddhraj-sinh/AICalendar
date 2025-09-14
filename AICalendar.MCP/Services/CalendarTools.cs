using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AICalendar.MCP.Services;

[McpServerToolType]
public class CalendarTools
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CalendarTools> _logger;

    public CalendarTools(IHttpClientFactory httpClientFactory, ILogger<CalendarTools> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [McpServerTool, Description("Retrieve calendar events from Microsoft Graph API via the Calendar API")]
    public async Task<string> GetCalendarEventsAsync(
        [Description("Start date for filtering events (optional, ISO 8601 format)")] string? start = null,
        [Description("End date for filtering events (optional, ISO 8601 format)")] string? end = null,
        [Description("Access token for authentication")] string? accessToken = null)
    {
        try
        {
            _logger.LogInformation("Getting calendar events with start: {Start}, end: {End}", start, end);

            var client = _httpClientFactory.CreateClient("CalendarApi");
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            // Build query string
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(start))
            {
                queryParams.Add($"start={Uri.EscapeDataString(start)}");
            }
            if (!string.IsNullOrEmpty(end))
            {
                queryParams.Add($"end={Uri.EscapeDataString(end)}");
            }

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await client.GetAsync($"/api/CalendarEvent{queryString}");

            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<List<CalendarEventDto>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (events == null || events.Count == 0)
                {
                    return "No calendar events found for the specified period.";
                }

                // Format response as JSON for better AI consumption
                var formattedEvents = events.Select(e => new
                {
                    title = e.Title,
                    start = e.Start.ToString("yyyy-MM-dd HH:mm"),
                    end = e.End.ToString("yyyy-MM-dd HH:mm"),
                    description = e.Description
                });

                return JsonSerializer.Serialize(new
                {
                    count = events.Count,
                    events = formattedEvents
                }, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get calendar events. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);

                return JsonSerializer.Serialize(new
                {
                    error = "Failed to retrieve calendar events",
                    statusCode = (int)response.StatusCode,
                    details = errorContent
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar events");
            return JsonSerializer.Serialize(new
            {
                error = "Error retrieving calendar events",
                message = ex.Message
            });
        }
    }
}