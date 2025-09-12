using AICalendar.Service.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarEventController : ControllerBase
    {
        private ICalendarEeventService _calendarEventService;
        public CalendarEventController(ICalendarEeventService calendarEeventService)
        {
            _calendarEventService = calendarEeventService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserEvents()
        {
            // Extract the access token from the Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Unauthorized();
            }
            var accessToken = authHeader.Substring("Bearer ".Length).Trim();
            var events = await _calendarEventService.GetUserEventsAsync(accessToken);
            return Ok(events);
        }
    }
}
