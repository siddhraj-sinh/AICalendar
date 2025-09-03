using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AICalendar.DomainModels.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DisplayName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties for calendar events (will be added later)
        // public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}