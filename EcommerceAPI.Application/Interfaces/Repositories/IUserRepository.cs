using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByRefreshToken(string refreshToken);
    Task<User?> GetByGoogleIdAsync(string googleId);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> ExistsByEmailAsync(string email);
}