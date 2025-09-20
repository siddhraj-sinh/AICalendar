using AICalendar.DomainModels.DTOs;
using AICalendar.Service.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace AICalendar.Service.Implementations
{
    public class CalendarEventService : ICalendarEeventService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CalendarEventService> _logger;

        public CalendarEventService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, ILogger<CalendarEventService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<CalendarEventDto> CreateEventAsync(CalendarEventDto calendarEvent)
        {
            _logger.LogInformation("[CreateEventAsync] Start creating calendar event: {Title}", calendarEvent.Title);

            try
            {
                var clientAccessToken = GetAccessToken();
                _logger.LogDebug("[CreateEventAsync] Retrieved access token from HTTP context.");

                // 1. Build confidential client for OBO
                var confidentialClient = ConfidentialClientApplicationBuilder
                    .Create(_configuration["AzureAd:ClientId"])       // CalendarApi ClientId
                    .WithClientSecret(_configuration["AzureAd:ClientSecret"]) // CalendarApi ClientSecret
                    .WithTenantId(_configuration["AzureAd:TenantId"])
                    .Build();

                _logger.LogDebug("[CreateEventAsync] Built confidential client for OBO flow.");

                // 2. UserAssertion from client token
                var userAssertion = new UserAssertion(clientAccessToken);

                // 3. Acquire token for Graph
                var result = await confidentialClient
                    .AcquireTokenOnBehalfOf(new[] { "https://graph.microsoft.com/.default" }, userAssertion)
                    .ExecuteAsync();

                var graphToken = result.AccessToken;
                _logger.LogDebug("[CreateEventAsync] Acquired Graph API token via OBO flow.");

                // 4. Call Graph API using new token
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", graphToken);

                // 5. Build the event object for Microsoft Graph API
                var graphEvent = new
                {
                    subject = calendarEvent.Title,
                    body = new
                    {
                        contentType = "HTML",
                        content = calendarEvent.Description ?? ""
                    },
                    start = new
                    {
                        dateTime = calendarEvent.Start.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                        timeZone = TimeZoneInfo.Local.Id
                    },
                    end = new
                    {
                        dateTime = calendarEvent.End.ToString("yyyy-MM-ddTHH:mm:ss.fffK"),
                        timeZone = TimeZoneInfo.Local.Id
                    },
                    location = !string.IsNullOrEmpty(calendarEvent.Location) ? new
                    {
                        displayName = calendarEvent.Location
                    } : null,
                    attendees = calendarEvent.Attendees?.Select(attendee => new
                    {
                        emailAddress = new
                        {
                            address = attendee.Email,
                            name = attendee.Name
                        },
                        type = attendee.Type.ToString().ToLower()
                    }).ToArray()
                };

                var jsonContent = JsonSerializer.Serialize(graphEvent, new JsonSerializerOptions 
                { 
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
                });
                var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("[CreateEventAsync] Sending POST request to Graph API to create event.");

                // 6. Make POST request to create the event
                var response = await client.PostAsync("me/events", httpContent);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[CreateEventAsync] Successfully created event. Response: {Response}", responseJson);

                // 7. Parse the response and return the created event
                using var doc = JsonDocument.Parse(responseJson);
                var createdEvent = new CalendarEventDto
                {
                    Id = doc.RootElement.GetProperty("id").GetString() ?? "",
                    Title = doc.RootElement.GetProperty("subject").GetString() ?? "",
                    Start = DateTime.Parse(doc.RootElement.GetProperty("start").GetProperty("dateTime").GetString() ?? ""),
                    End = DateTime.Parse(doc.RootElement.GetProperty("end").GetProperty("dateTime").GetString() ?? ""),
                    Description = doc.RootElement.TryGetProperty("bodyPreview", out var desc) ? desc.GetString() : null,
                    Location = doc.RootElement.TryGetProperty("location", out var location) ? 
                        location.TryGetProperty("displayName", out var displayName) ? displayName.GetString() : null : null
                };

                // Parse organizer
                if (doc.RootElement.TryGetProperty("organizer", out var organizer))
                {
                    createdEvent.Organizer = new OrganizerDto
                    {
                        Name = organizer.TryGetProperty("emailAddress", out var orgEmail) && orgEmail.TryGetProperty("name", out var orgName) ? orgName.GetString() : null,
                        Email = organizer.TryGetProperty("emailAddress", out var orgEmailAddr) && orgEmailAddr.TryGetProperty("address", out var orgAddr) ? orgAddr.GetString() : null
                    };
                }

                // Parse attendees
                if (doc.RootElement.TryGetProperty("attendees", out var attendees) && attendees.ValueKind == JsonValueKind.Array)
                {
                    createdEvent.Attendees = new List<AttendeeDto>();
                    foreach (var attendee in attendees.EnumerateArray())
                    {
                        var attendeeDto = new AttendeeDto();
                        
                        if (attendee.TryGetProperty("emailAddress", out var attEmail))
                        {
                            attendeeDto.Name = attEmail.TryGetProperty("name", out var attName) ? attName.GetString() : null;
                            attendeeDto.Email = attEmail.TryGetProperty("address", out var attAddr) ? attAddr.GetString() : null;
                        }

                        if (attendee.TryGetProperty("type", out var type))
                        {
                            attendeeDto.Type = type.GetString()?.ToLower() switch
                            {
                                "optional" => AttendeeType.Optional,
                                "resource" => AttendeeType.Resource,
                                _ => AttendeeType.Required
                            };
                        }

                        if (attendee.TryGetProperty("status", out var attendeeStatus) && attendeeStatus.TryGetProperty("response", out var attendeeResponse))
                        {
                            attendeeDto.ResponseStatus = attendeeResponse.GetString()?.ToLower() switch
                            {
                                "accepted" => AttendeeResponseStatus.Accepted,
                                "declined" => AttendeeResponseStatus.Declined,
                                "tentativelyaccepted" => AttendeeResponseStatus.TentativelyAccepted,
                                "organizer" => AttendeeResponseStatus.Organizer,
                                _ => AttendeeResponseStatus.NotResponded
                            };
                        }

                        createdEvent.Attendees.Add(attendeeDto);
                    }
                }

                _logger.LogInformation("[CreateEventAsync] Successfully created calendar event with ID: {EventId}", createdEvent.Id);
                return createdEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CreateEventAsync] Error creating calendar event: {Title}", calendarEvent.Title);
                throw;
            }
        }

        // Pass the access token from the client as a parameter
        public async Task<List<CalendarEventDto>> GetEventsAsync(DateTime? start, DateTime? end)
        {
            try
            {
                var clientAccessToken = GetAccessToken();
                // 1. Build confidential client for OBO
                 var confidentialClient = ConfidentialClientApplicationBuilder
                    .Create(_configuration["AzureAd:ClientId"])       // CalendarApi ClientId
                    .WithClientSecret(_configuration["AzureAd:ClientSecret"]) // CalendarApi ClientSecret
                    .WithTenantId(_configuration["AzureAd:TenantId"])
                    .Build();

                // 2. UserAssertion from client token
                var userAssertion = new UserAssertion(clientAccessToken);

                // 3. Acquire token for Graph
                var result = await confidentialClient
                    .AcquireTokenOnBehalfOf(new[] { "https://graph.microsoft.com/.default" }, userAssertion)
                    .ExecuteAsync();

                var graphToken = result.AccessToken;

                // 4. Call Graph API using new token
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://graph.microsoft.com/v1.0/");
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", graphToken);

                // 5. Build query using the new method
                var url = BuildQueryUrl(start, end);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var events = new List<CalendarEventDto>();
                using var doc = JsonDocument.Parse(json);
                foreach (var item in doc.RootElement.GetProperty("value").EnumerateArray())
                {
                    var calendarEvent = new CalendarEventDto
                    {
                        Id = item.GetProperty("id").GetString() ?? "",
                        Title = item.GetProperty("subject").GetString() ?? "",
                        Start = DateTime.Parse(item.GetProperty("start").GetProperty("dateTime").GetString() ?? ""),
                        End = DateTime.Parse(item.GetProperty("end").GetProperty("dateTime").GetString() ?? ""),
                        Description = item.TryGetProperty("bodyPreview", out var desc) ? desc.GetString() : null,
                        Location = item.TryGetProperty("location", out var location) ? 
                            location.TryGetProperty("displayName", out var displayName) ? displayName.GetString() : null : null,
                    };

                 
                    // Parse organizer
                    if (item.TryGetProperty("organizer", out var organizer))
                    {
                        calendarEvent.Organizer = new OrganizerDto
                        {
                            Name = organizer.TryGetProperty("emailAddress", out var orgEmail) && orgEmail.TryGetProperty("name", out var orgName) ? orgName.GetString() : null,
                            Email = organizer.TryGetProperty("emailAddress", out var orgEmailAddr) && orgEmailAddr.TryGetProperty("address", out var orgAddr) ? orgAddr.GetString() : null
                        };
                    }

                    // Parse attendees
                    if (item.TryGetProperty("attendees", out var attendees) && attendees.ValueKind == JsonValueKind.Array)
                    {
                        calendarEvent.Attendees = new List<AttendeeDto>();
                        foreach (var attendee in attendees.EnumerateArray())
                        {
                            var attendeeDto = new AttendeeDto();
                            
                            if (attendee.TryGetProperty("emailAddress", out var attEmail))
                            {
                                attendeeDto.Name = attEmail.TryGetProperty("name", out var attName) ? attName.GetString() : null;
                                attendeeDto.Email = attEmail.TryGetProperty("address", out var attAddr) ? attAddr.GetString() : null;
                            }

                            if (attendee.TryGetProperty("type", out var type))
                            {
                                attendeeDto.Type = type.GetString()?.ToLower() switch
                                {
                                    "optional" => AttendeeType.Optional,
                                    "resource" => AttendeeType.Resource,
                                    _ => AttendeeType.Required
                                };
                            }

                            if (attendee.TryGetProperty("status", out var attendeeStatus) && attendeeStatus.TryGetProperty("response", out var attendeeResponse))
                            {
                                attendeeDto.ResponseStatus = attendeeResponse.GetString()?.ToLower() switch
                                {
                                    "accepted" => AttendeeResponseStatus.Accepted,
                                    "declined" => AttendeeResponseStatus.Declined,
                                    "tentativelyaccepted" => AttendeeResponseStatus.TentativelyAccepted,
                                    "organizer" => AttendeeResponseStatus.Organizer,
                                    _ => AttendeeResponseStatus.NotResponded
                                };
                            }

                            calendarEvent.Attendees.Add(attendeeDto);
                        }
                    }
                    events.Add(calendarEvent);
                }
                return events;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar events");
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private string BuildQueryUrl(DateTime? start, DateTime? end)
        {
            var startDate = start ?? DateTime.Today;
            var endDate = end ?? DateTime.Today.AddDays(30);

            // Use CalendarView for date range queries (Microsoft recommended approach)
            if (start.HasValue || end.HasValue)
            {
                var startParam = Uri.EscapeDataString(startDate.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
                var endParam = Uri.EscapeDataString(endDate.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
                var select = "id,subject,start,end,bodyPreview,location,attendees,organizer,importance,sensitivity,showAs,categories,webLink,isAllDay,isCancelled,createdDateTime,lastModifiedDateTime";

                return $"me/calendarview?startDateTime={startParam}&endDateTime={endParam}&$select={select}&$orderby=start/dateTime&$top=100";
            }

            // Fallback to regular events endpoint if no date filtering needed
            return "me/events?$select=id,subject,start,end,bodyPreview,location,attendees,organizer,importance,sensitivity,showAs,categories,webLink,isAllDay,isCancelled,createdDateTime,lastModifiedDateTime&$orderby=start/dateTime&$top=100";
        }

        private string? GetAccessToken()
        {
            var authHeader = _httpContextAccessor.HttpContext?
                .Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader.Substring("Bearer ".Length).Trim();
        }
    }
}