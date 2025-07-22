using Microsoft.IdentityModel.Tokens;
using SecureApiVAPT.DTOs;
using SecureApiVAPT.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SecureApiVAPT.Services;

public class UserService : IUserService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;
    private readonly List<User> _users = new(); // In-memory storage for demo

    public UserService(IConfiguration configuration, ILogger<UserService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await Task.FromResult(_users.FirstOrDefault(u => u.Id == id && u.IsActive));
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        return await Task.FromResult(_users.FirstOrDefault(u => u.Username == username && u.IsActive));
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await Task.FromResult(_users.FirstOrDefault(u => u.Email == email && u.IsActive));
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await Task.FromResult(_users.Where(u => u.IsActive));
    }

    public async Task<User> CreateUserAsync(UserDto userDto)
    {
        var existingUser = await GetUserByUsernameAsync(userDto.Username);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Username already exists");
        }

        var existingEmail = await GetUserByEmailAsync(userDto.Email);
        if (existingEmail != null)
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Id = _users.Count + 1,
            Username = userDto.Username,
            Email = userDto.Email,
            PasswordHash = HashPassword(userDto.Password),
            Age = userDto.Age,
            Roles = new List<string> { "User" }
        };

        _users.Add(user);
        _logger.LogInformation("User created: {Username}", user.Username);
        
        return await Task.FromResult(user);
    }

    public async Task<bool> UpdateUserAsync(int id, UserDto userDto)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null) return false;

        user.Username = userDto.Username;
        user.Email = userDto.Email;
        user.Age = userDto.Age;
        user.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("User updated: {Username}", user.Username);
        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null) return false;

        user.IsActive = false;
        _logger.LogInformation("User deactivated: {Username}", user.Username);
        return true;
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await GetUserByUsernameAsync(username);
        if (user == null) return false;

        var hashedPassword = HashPassword(password);
        var isValid = user.PasswordHash == hashedPassword;

        if (isValid)
        {
            user.LastLoginAt = DateTime.UtcNow;
            _logger.LogInformation("User logged in: {Username}", username);
        }
        else
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", username);
        }

        return isValid;
    }

    public async Task<string> GenerateJwtTokenAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return await Task.FromResult(tokenHandler.WriteToken(token));
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return false;

        var currentHash = HashPassword(currentPassword);
        if (user.PasswordHash != currentHash) return false;

        user.PasswordHash = HashPassword(newPassword);
        _logger.LogInformation("Password changed for user: {Username}", user.Username);
        return true;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
} 