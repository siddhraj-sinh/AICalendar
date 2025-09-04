using AICalendar.DomainModels.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICalendar.Service.Contracts
{
    public interface IAuthService
    {
        Task<RegisterUserResponseDto> RegisterUserAsync(RegisterUserRequestDto signUpRequestDto);

        Task<LoginUserResponseDto> LoginUserAsync(LoginUserRequestDto loginUserRequestDto);
    }
}
