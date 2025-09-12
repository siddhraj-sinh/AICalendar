namespace AICalendar.Client.DTOs
{
    public class CalendarEventDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string? Description { get; set; }
    }
}
