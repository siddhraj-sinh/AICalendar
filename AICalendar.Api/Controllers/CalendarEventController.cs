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
        public CalendarEventController(ICalendarEeventService calendarEeventService)
        {
            _calendarEventService = calendarEeventService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserEvents([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            // Extract the access token from the Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            var accessToken = authHeader.Substring("Bearer ".Length).Trim();

            var events = await _calendarEventService.GetUserEventsAsync(accessToken);
            return Ok(events);
        }
    }
}
