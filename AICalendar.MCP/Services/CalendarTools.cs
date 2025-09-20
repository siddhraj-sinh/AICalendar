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
                    description = e.Description,
                    location = e.Location,
                    organizer = e.Organizer != null ? new
                    {
                        name = e.Organizer.Name,
                        email = e.Organizer.Email
                    } : null,
                    attendees = e.Attendees?.Select(a => new
                    {
                        name = a.Name,
                        email = a.Email,
                        type = a.Type.ToString(),
                        responseStatus = a.ResponseStatus.ToString()
                    }).ToArray()
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

    [McpServerTool, Description("Create a new calendar event via the Calendar API")]
    public async Task<string> CreateCalendarEventAsync(
        [Description("Event title/subject (required)")] string title,
        [Description("Event start date and time (required, ISO 8601 format: YYYY-MM-DDTHH:mm:ss)")] string startDateTime,
        [Description("Event end date and time (required, ISO 8601 format: YYYY-MM-DDTHH:mm:ss)")] string endDateTime,
        [Description("Event description (optional)")] string? description = null,
        [Description("Event location (optional)")] string? location = null,
        [Description("Attendees emails comma-separated (optional, e.g., 'john@example.com,jane@example.com')")] string? attendeeEmails = null,
        [Description("Access token for authentication")] string? accessToken = null)
    {
        try
        {
            _logger.LogInformation("Creating calendar event with title: {Title}, start: {Start}, end: {End}", 
                title, startDateTime, endDateTime);

            // Validate required parameters
            if (string.IsNullOrEmpty(title))
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Title is required",
                    message = "Event title cannot be empty"
                });
            }

            if (string.IsNullOrEmpty(startDateTime) || string.IsNullOrEmpty(endDateTime))
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Start and end date times are required",
                    message = "Both startDateTime and endDateTime must be provided in ISO 8601 format"
                });
            }

            // Validate date formats
            if (!DateTime.TryParse(startDateTime, out var start) || !DateTime.TryParse(endDateTime, out var end))
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Invalid date format",
                    message = "Please provide dates in ISO 8601 format (YYYY-MM-DDTHH:mm:ss)"
                });
            }

            // Validate that end is after start
            if (end <= start)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Invalid date range",
                    message = "End date/time must be after start date/time"
                });
            }

            var client = _httpClientFactory.CreateClient("CalendarApi");
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            // Parse attendees if provided
            List<AttendeeDto>? attendees = null;
            if (!string.IsNullOrEmpty(attendeeEmails))
            {
                var emailList = attendeeEmails.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(email => email.Trim())
                    .Where(email => !string.IsNullOrEmpty(email));

                attendees = emailList.Select(email => new AttendeeDto
                {
                    Email = email,
                    Name = email.Split('@')[0], // Use part before @ as name
                    Type = AttendeeType.Required,
                    ResponseStatus = AttendeeResponseStatus.None
                }).ToList();
            }

            // Create the calendar event object with all fields
            var calendarEvent = new CalendarEventDto
            {
                Title = title,
                Start = start,
                End = end,
                Description = description,
                Location = location,
                Attendees = attendees
            };

            var jsonContent = JsonSerializer.Serialize(calendarEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending POST request to create calendar event");

            var response = await client.PostAsync("/api/CalendarEvent/create", httpContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createdEvent = JsonSerializer.Deserialize<CalendarEventDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (createdEvent != null)
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        message = "Calendar event created successfully",
                        eventData = new
                        {
                            id = createdEvent.Id,
                            title = createdEvent.Title,
                            start = createdEvent.Start.ToString("yyyy-MM-dd HH:mm"),
                            end = createdEvent.End.ToString("yyyy-MM-dd HH:mm"),
                            description = createdEvent.Description,
                            location = createdEvent.Location,
                            attendees = createdEvent.Attendees?.Select(a => new
                            {
                                name = a.Name,
                                email = a.Email,
                                type = a.Type.ToString(),
                                responseStatus = a.ResponseStatus.ToString()
                            }).ToArray(),
                            organizer = createdEvent.Organizer != null ? new
                            {
                                name = createdEvent.Organizer.Name,
                                email = createdEvent.Organizer.Email
                            } : null
                        }
                    }, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                }
                else
                {
                    return JsonSerializer.Serialize(new
                    {
                        success = true,
                        message = "Calendar event created successfully",
                        rawResponse = responseContent
                    });
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to create calendar event. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);

                return JsonSerializer.Serialize(new
                {
                    error = "Failed to create calendar event",
                    statusCode = (int)response.StatusCode,
                    details = errorContent
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating calendar event");
            return JsonSerializer.Serialize(new
            {
                error = "Error creating calendar event",
                message = ex.Message
            });
        }
    }
}