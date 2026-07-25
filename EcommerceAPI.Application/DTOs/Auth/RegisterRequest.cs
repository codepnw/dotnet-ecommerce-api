using EcommerceAPI.Domain.Enums;

namespace EcommerceAPI.Application.DTOs.Requests;

public class RegisterRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string Role { get; set; } = UserRoles.User;
}
