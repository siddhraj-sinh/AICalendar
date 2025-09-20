using AICalendar.DomainModels.DTOs;
using AICalendar.Service.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;

namespace AICalendar.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [RequiredScope("access_as_user")]
    public class CalendarEventController : ControllerBase
    {
        private ICalendarEeventService _calendarEventService;
        private readonly ILogger<CalendarEventController> _logger;
        public CalendarEventController(ICalendarEeventService calendarEeventService, ILogger<CalendarEventController> logger)
        {
            _calendarEventService = calendarEeventService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetEventsAsync([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {

            var events = await _calendarEventService.GetEventsAsync(start,end);
            return Ok(events);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateEventAsync([FromBody] CalendarEventDto calendarEvent)
        {
            if (calendarEvent == null)
            {
                return BadRequest("Event data is required.");
            }
            try
            {
                var createdEvent = await _calendarEventService.CreateEventAsync(calendarEvent);
                return Ok(createdEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return BadRequest("Error creating event");
            }
        }
    }
}
