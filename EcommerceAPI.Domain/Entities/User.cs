using EcommerceAPI.Domain.Common;
using EcommerceAPI.Domain.Enums;

namespace EcommerceAPI.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.User;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // Google OAuth Fields
    public string? GoogleId { get; set; }
    public string? PictureUrl { get; set; }
    public string? DisplayName { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}