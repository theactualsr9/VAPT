using Microsoft.IdentityModel.Tokens;
using SecureApiVAPT.DTOs;
using SecureApiVAPT.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SecureApiVAPT.Data;
using Microsoft.Data.SqlClient;

namespace SecureApiVAPT.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserService> _logger;

    public UserService(ApplicationDbContext context, IConfiguration configuration, ILogger<UserService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        using var reader = await _context.ExecuteStoredProcedureAsync("sp_GetUserById", 
            _context.CreateParameter("@UserId", id));
        
        if (await reader.ReadAsync())
        {
            return MapUserFromReader(reader);
        }
        return null;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        using var reader = await _context.ExecuteStoredProcedureAsync("sp_GetUserByUsername", 
            _context.CreateParameter("@Username", username));
        
        if (await reader.ReadAsync())
        {
            return MapUserFromReader(reader);
        }
        return null;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var reader = await _context.ExecuteStoredProcedureAsync("sp_GetUserByEmail", 
            _context.CreateParameter("@Email", email));
        
        if (await reader.ReadAsync())
        {
            return MapUserFromReader(reader);
        }
        return null;
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        using var reader = await _context.ExecuteStoredProcedureAsync("sp_GetAllUsers");
        
        var users = new List<User>();
        while (await reader.ReadAsync())
        {
            users.Add(MapUserFromReader(reader));
        }
        return users;
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
            Username = userDto.Username,
            Email = userDto.Email,
            PasswordHash = HashPassword(userDto.Password),
            Age = userDto.Age,
            Roles = new List<string> { "User" }
        };

        var result = await _context.ExecuteStoredProcedureScalarAsync("sp_CreateUser",
            _context.CreateParameter("@Username", user.Username),
            _context.CreateParameter("@Email", user.Email),
            _context.CreateParameter("@PasswordHash", user.PasswordHash),
            _context.CreateParameter("@Age", user.Age),
            _context.CreateParameter("@Roles", string.Join(",", user.Roles)));

        if (result != null)
        {
            user.Id = Convert.ToInt32(result);
        }

        _logger.LogInformation("User created: {Username}", user.Username);
        return user;
    }

    public async Task<bool> UpdateUserAsync(int id, UserDto userDto)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null) return false;

        user.Username = userDto.Username;
        user.Email = userDto.Email;
        user.Age = userDto.Age;

        await _context.ExecuteStoredProcedureNonQueryAsync("sp_UpdateUser",
            _context.CreateParameter("@UserId", user.Id),
            _context.CreateParameter("@Username", user.Username),
            _context.CreateParameter("@Email", user.Email),
            _context.CreateParameter("@Age", user.Age));

        _logger.LogInformation("User updated: {Username}", user.Username);
        return true;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        var user = await GetUserByIdAsync(id);
        if (user == null) return false;

        await _context.ExecuteStoredProcedureNonQueryAsync("sp_DeleteUser",
            _context.CreateParameter("@UserId", id));

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
            await _context.ExecuteStoredProcedureNonQueryAsync("sp_UpdateLastLogin",
                _context.CreateParameter("@UserId", user.Id));

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

        var newHash = HashPassword(newPassword);
        await _context.ExecuteStoredProcedureNonQueryAsync("sp_ChangePassword",
            _context.CreateParameter("@UserId", userId),
            _context.CreateParameter("@NewPasswordHash", newHash));

        _logger.LogInformation("Password changed for user: {Username}", user.Username);
        return true;
    }

    private static User MapUserFromReader(SqlDataReader reader)
    {
        return new User
        {
            Id = reader.GetInt32("Id"),
            Username = reader.GetString("Username"),
            Email = reader.GetString("Email"),
            PasswordHash = reader.GetString("PasswordHash"),
            Age = reader.GetInt32("Age"),
            CreatedAt = reader.GetDateTime("CreatedAt"),
            LastLoginAt = reader.IsDBNull("LastLoginAt") ? null : reader.GetDateTime("LastLoginAt"),
            IsActive = reader.GetBoolean("IsActive"),
            Roles = reader.GetString("Roles").Split(',').ToList()
        };
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
} 