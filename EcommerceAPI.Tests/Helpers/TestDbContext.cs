using EcommerceAPI.Application.Commons.Constrants;
using EcommerceAPI.Infrastructure;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Tests.Helpers;

public static class TestDbContext
{
    public static EcommerceDbContext Create()
    {
        var options = new DbContextOptionsBuilder<EcommerceDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new EcommerceDbContext(options, null);
        context.Database.EnsureCreated();

        return context;
    }

    public static EcommerceDbContext CreateWithUsers()
    {
        var context = Create();

        context.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminTest!"),
                Role = UserRoles.Admin,
                RefreshToken = "valid-refresh-token",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
            },
            new User
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserTest!"),
                Role = UserRoles.User,
                RefreshToken = null,
                RefreshTokenExpiry = null
            }
        );

        context.SaveChanges();
        return context;
    }
}