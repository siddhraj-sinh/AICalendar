using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICalendar.MCP.Services
{
    public class CalendarEventDto
    {
        public string? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Description { get; set; }
        public string? Location { get; set; }
        public List<AttendeeDto>? Attendees { get; set; }
        public OrganizerDto? Organizer { get; set; }
    }

    public class AttendeeDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public AttendeeResponseStatus ResponseStatus { get; set; } = AttendeeResponseStatus.None;
        public AttendeeType Type { get; set; } = AttendeeType.Required;
    }

    public class OrganizerDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    public enum AttendeeResponseStatus
    {
        None,
        Organizer,
        TentativelyAccepted,
        Accepted,
        Declined,
        NotResponded
    }

    public enum AttendeeType
    {
        Required,
        Optional,
        Resource
    }

}
