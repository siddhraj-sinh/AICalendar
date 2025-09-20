using AICalendar.DomainModels.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICalendar.Service.Contracts
{
    public interface ICalendarEeventService
    {
        Task<List<CalendarEventDto>> GetEventsAsync(DateTime? start, DateTime? end);

        Task<CalendarEventDto> CreateEventAsync(CalendarEventDto calendarEvent);
    }
}
