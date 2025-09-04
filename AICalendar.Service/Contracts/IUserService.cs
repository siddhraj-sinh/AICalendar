using System;
using System.Threading.Tasks;
using AICalendar.DomainModels.Models;

namespace AICalendar.Service.Contracts
{
    public interface IUserService
    {
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
    }
}
