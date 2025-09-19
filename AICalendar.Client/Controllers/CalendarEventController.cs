using AICalendar.Client.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;
using System.Globalization;

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
        public async Task<IActionResult> GetEvents([FromQuery] string? start, [FromQuery] string? end)
        {
            try
            {
                _logger.LogInformation("Fetching calendar events. Start: {Start}, End: {End}", start, end);

                // Build query string for the API call
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
                var relativePath = $"/api/CalendarEvent{queryString}";

                _logger.LogInformation("Calling API with path: {RelativePath}", relativePath);

                // Call your Web API with query parameters
                var events = await _downstreamApi.CallApiForUserAsync<List<CalendarEventDto>>("CalendarApi", options =>
                {
                    options.RelativePath = relativePath;
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
