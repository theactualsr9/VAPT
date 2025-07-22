using SecureApiVAPT.DTOs;
using SecureApiVAPT.Models;

namespace SecureApiVAPT.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int id);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(UserDto userDto);
    Task<bool> UpdateUserAsync(int id, UserDto userDto);
    Task<bool> DeleteUserAsync(int id);
    Task<bool> ValidateCredentialsAsync(string username, string password);
    Task<string> GenerateJwtTokenAsync(User user);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
} 