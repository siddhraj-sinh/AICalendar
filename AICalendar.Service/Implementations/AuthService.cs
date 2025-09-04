using AICalendar.DomainModels.DTOs;
using AICalendar.DomainModels.Models;
using AICalendar.Service.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AICalendar.Service.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<ApplicationUser> userManager, 
                          SignInManager<ApplicationUser> signInManager, 
                          IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<LoginUserResponseDto> LoginUserAsync(LoginUserRequestDto loginUserRequestDto)
        {
            try
            {
                // Find user by email
                var user = await _userManager.FindByEmailAsync(loginUserRequestDto.Email);
                if (user == null)
                {
                    return new LoginUserResponseDto
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Invalid login credentials" }
                    };
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return new LoginUserResponseDto
                    {
                        IsSuccess = false,
                        Message = "Account is deactivated",
                        Errors = new List<string> { "Your account has been deactivated. Please contact support." }
                    };
                }

                // Attempt to sign in
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginUserRequestDto.Password, false);

                if (result.Succeeded)
                {
                    // Generate JWT token
                    var token = GenerateJwtToken(user);
                    var tokenExpiration = DateTime.UtcNow.AddHours(24); // Token expires in 24 hours

                    // Update last login time
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    return new LoginUserResponseDto
                    {
                        IsSuccess = true,
                        Message = "Login successful",
                        Token = token,
                        TokenExpiration = tokenExpiration,
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            FullName = user.FullName,
                            DisplayName = user.DisplayName,
                            CreatedAt = user.CreatedAt,
                            IsActive = user.IsActive
                        }
                    };
                }
                else if (result.IsLockedOut)
                {
                    return new LoginUserResponseDto
                    {
                        IsSuccess = false,
                        Message = "Account locked",
                        Errors = new List<string> { "Your account has been locked due to multiple failed login attempts." }
                    };
                }
                else
                {
                    return new LoginUserResponseDto
                    {
                        IsSuccess = false,
                        Message = "Invalid email or password",
                        Errors = new List<string> { "Invalid login credentials" }
                    };
                }
            }
            catch (Exception ex)
            {
                return new LoginUserResponseDto
                {
                    IsSuccess = false,
                    Message = "An error occurred during login",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? "AICalendar_SecretKey_2024_MinimumLength32Characters!";
            var issuer = jwtSettings["Issuer"] ?? "AICalendar.Api";
            var audience = jwtSettings["Audience"] ?? "AICalendar.Client";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("FullName", user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RegisterUserResponseDto> RegisterUserAsync(RegisterUserRequestDto registerUserRequestDto)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(registerUserRequestDto.Email);
                if (existingUser != null)
                {
                    return new RegisterUserResponseDto
                    {
                        IsSuccess = false,
                        Message = "User registration failed",
                        Errors = new List<string> { "User with this email already exists" }
                    };
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = registerUserRequestDto.Email,
                    Email = registerUserRequestDto.Email,
                    FirstName = registerUserRequestDto.FirstName,
                    LastName = registerUserRequestDto.LastName,
                    DisplayName = registerUserRequestDto.DisplayName,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Create user with password
                var result = await _userManager.CreateAsync(user, registerUserRequestDto.Password);

                if (result.Succeeded)
                {
                    return new RegisterUserResponseDto
                    {
                        IsSuccess = true,
                        Message = "User registered successfully",
                        Errors = new List<string>(),
                        User = new UserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            FullName = user.FullName,
                            DisplayName = user.DisplayName,
                            CreatedAt = user.CreatedAt,
                            IsActive = user.IsActive
                        }
                    };
                }
                else
                {
                    return new RegisterUserResponseDto
                    {
                        IsSuccess = false,
                        Message = "User registration failed",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    };
                }
            }
            catch (Exception ex)
            {
                return new RegisterUserResponseDto
                {
                    IsSuccess = false,
                    Message = "An error occurred during user registration",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
    }
}
