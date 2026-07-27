using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Application.Interfaces;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Infrastructure.Repositories;

public class UserRepository(EcommerceDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByGoogleIdAsync(string googleId)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }

    public async Task<User?> GetByRefreshToken(string refreshToken)
    {
        return await context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    }

    public async Task<User> CreateAsync(User user)
    {
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await context.Users.AnyAsync(u => u.Email == email);
    }
}