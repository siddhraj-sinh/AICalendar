using AICalendar.Client.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;

namespace AICalendar.Client.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarEventController : ControllerBase   
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly ILogger<CalendarEventController> _logger;

        public CalendarEventController(IDownstreamApi downstreamApi, ILogger<CalendarEventController> logger)
        {
            _downstreamApi = downstreamApi;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetEvents([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            try
            {
                _logger.LogInformation("Fetching calendar events. Start: {Start}, End: {End}", start, end);

                // Call your Web API
                var events = await _downstreamApi.CallApiForUserAsync<List<CalendarEventDto>>("CalendarApi", options =>
                {
                    options.RelativePath = "/api/CalendarEvent";
                });

                if (events == null)
                {
                    _logger.LogWarning("No events returned from API");
                    return Ok(new List<object>());
                }

                _logger.LogInformation("Retrieved {Count} events from API", events.Count);

                // Convert to FullCalendar format
                var fullCalendarEvents = events.Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    start = e.Start.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = e.End.ToString("yyyy-MM-ddTHH:mm:ss"),
                    description = e.Description,
                    // Add any other properties FullCalendar might need
                    allDay = false // Set to true if it's an all-day event
                }).ToList();

                // Optional: Filter by date range if provided by FullCalendar
                if (start.HasValue && end.HasValue)
                {
                    var filteredEvents = fullCalendarEvents.Where(e =>
                    {
                        if (DateTime.TryParse(e.start, out DateTime eventStart))
                        {
                            return eventStart >= start.Value && eventStart <= end.Value;
                        }
                        return true; // Include events with unparseable dates
                    }).ToList();

                    _logger.LogInformation("Filtered to {Count} events for date range", filteredEvents.Count);
                    return Ok(filteredEvents);
                }

                return Ok(fullCalendarEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching calendar events");
                return StatusCode(500, new { message = "Failed to fetch events", error = ex.Message });
            }
        }
    }
}
