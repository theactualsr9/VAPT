using System.ComponentModel.DataAnnotations;
using SecureApiVAPT.Attributes;

namespace SecureApiVAPT.DTOs;

public class UserDto
{
    [Required]
    [StringLength(100, MinimumLength = 3)]
    [NoXss]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [NoXss]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string Password { get; set; } = string.Empty;

    [Range(18, 120)]
    public int Age { get; set; }
}

public class LoginDto
{
    [Required]
    [NoXss]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class ProductDto
{
    [Required]
    [StringLength(200)]
    [NoXss]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [StringLength(1000)]
    [NoXss]
    public string Description { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }
} 