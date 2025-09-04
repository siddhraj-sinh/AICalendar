using AICalendar.DomainModels.DTOs;
using AICalendar.Service.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace AICalendar.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(ILogger<AuthController> logger, IAuthService authService)
        {
            _logger = logger;
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUserAsync([FromBody] LoginUserRequestDto loginUserRequestDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                var result = await _authService.LoginUserAsync(loginUserRequestDto);
                if (result.IsSuccess)
                {
                    _logger.LogInformation("User {Email} logged in successfully", loginUserRequestDto.Email);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("Login failed for {Email}: {Errors}", 
                        loginUserRequestDto.Email, string.Join(", ", result.Errors));
                    return Unauthorized(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for {Email}", loginUserRequestDto.Email);
                return StatusCode(500, new LoginUserResponseDto
                {
                    IsSuccess = false,
                    Message = "Internal server error occurred",
                    Errors = new List<string> { "Please try again later" }
                });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterUserRequestDto registerUserRequestDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterUserAsync(registerUserRequestDto);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("User {Email} registered successfully", registerUserRequestDto.Email);
                    return Ok(result);
                }
                else
                {
                    _logger.LogWarning("User registration failed for {Email}: {Errors}", 
                        registerUserRequestDto.Email, string.Join(", ", result.Errors));
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during user registration for {Email}", registerUserRequestDto.Email);
                return StatusCode(500, new RegisterUserResponseDto
                {
                    IsSuccess = false,
                    Message = "Internal server error occurred",
                    Errors = new List<string> { "Please try again later" }
                });
            }
        }
    }
}
