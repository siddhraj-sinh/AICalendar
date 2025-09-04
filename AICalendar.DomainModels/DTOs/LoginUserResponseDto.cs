using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICalendar.DomainModels.DTOs
{
    public class LoginUserResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
        public string? Token { get; set; }
        public DateTime? TokenExpiration { get; set; }
        public UserDto? User { get; set; }
    }
}
