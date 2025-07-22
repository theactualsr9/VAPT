using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureApiVAPT.DTOs;
using SecureApiVAPT.Services;
using System.ComponentModel.DataAnnotations;

namespace SecureApiVAPT.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] UserDto userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _userService.CreateUserAsync(userDto);
            var token = await _userService.GenerateJwtTokenAsync(user);

            _logger.LogInformation("User registered successfully: {Username}", user.Username);

            return CreatedAtAction(nameof(Login), new { username = user.Username }, new
            {
                User = new { user.Id, user.Username, user.Email, user.Age },
                Token = token
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var isValid = await _userService.ValidateCredentialsAsync(loginDto.Username, loginDto.Password);
        if (!isValid)
        {
            _logger.LogWarning("Login failed for user: {Username}", loginDto.Username);
            return Unauthorized(new { Error = "Invalid credentials" });
        }

        var user = await _userService.GetUserByUsernameAsync(loginDto.Username);
        if (user == null)
        {
            return Unauthorized(new { Error = "User not found" });
        }

        var token = await _userService.GenerateJwtTokenAsync(user);

        _logger.LogInformation("User logged in successfully: {Username}", user.Username);

        return Ok(new
        {
            User = new { user.Id, user.Username, user.Email, user.Age },
            Token = token
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId == 0)
        {
            return Unauthorized();
        }

        var success = await _userService.ChangePasswordAsync(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        if (!success)
        {
            return BadRequest(new { Error = "Current password is incorrect" });
        }

        _logger.LogInformation("Password changed successfully for user ID: {UserId}", userId);

        return Ok(new { Message = "Password changed successfully" });
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        if (userId == 0)
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { Error = "User not found" });
        }

        return Ok(new
        {
            user.Id,
            user.Username,
            user.Email,
            user.Age,
            user.CreatedAt,
            user.LastLoginAt
        });
    }
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string NewPassword { get; set; } = string.Empty;
} 