using System;
using System.Threading.Tasks;
using AICalendar.DomainModels.Models;
using AICalendar.Service.Contracts;
using Microsoft.AspNetCore.Identity;

namespace AICalendar.Service.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _userManager.FindByIdAsync(userId);
        }
    }
}
