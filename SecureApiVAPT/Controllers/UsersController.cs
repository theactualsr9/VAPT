using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureApiVAPT.DTOs;
using SecureApiVAPT.Services;

namespace SecureApiVAPT.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        
        var userDtos = users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.Age,
            u.CreatedAt,
            u.LastLoginAt,
            u.IsActive,
            u.Roles
        });

        return Ok(userDtos);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
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
            user.LastLoginAt,
            user.IsActive,
            user.Roles
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _userService.UpdateUserAsync(id, userDto);
        if (!success)
        {
            return NotFound(new { Error = "User not found" });
        }

        _logger.LogInformation("User updated by admin: {UserId}", id);

        return Ok(new { Message = "User updated successfully" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var success = await _userService.DeleteUserAsync(id);
        if (!success)
        {
            return NotFound(new { Error = "User not found" });
        }

        _logger.LogInformation("User deleted by admin: {UserId}", id);

        return Ok(new { Message = "User deleted successfully" });
    }

    [HttpGet("search")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchUsers([FromQuery] string? username, [FromQuery] string? email)
    {
        var users = await _userService.GetAllUsersAsync();
        
        if (!string.IsNullOrWhiteSpace(username))
        {
            users = users.Where(u => u.Username.Contains(username, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            users = users.Where(u => u.Email.Contains(email, StringComparison.OrdinalIgnoreCase));
        }

        var userDtos = users.Select(u => new
        {
            u.Id,
            u.Username,
            u.Email,
            u.Age,
            u.CreatedAt,
            u.LastLoginAt,
            u.IsActive
        });

        return Ok(userDtos);
    }

    [HttpGet("public-search")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublicSearchUsers([FromQuery] string? username)
    {
        // This endpoint is for security testing only - returns minimal info
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { Error = "Username parameter is required" });
        }

        var users = await _userService.GetAllUsersAsync();
        var matchingUsers = users.Where(u => u.Username.Contains(username, StringComparison.OrdinalIgnoreCase))
                                 .Select(u => new { u.Username })
                                 .Take(5); // Limit results

        return Ok(matchingUsers);
    }
} 