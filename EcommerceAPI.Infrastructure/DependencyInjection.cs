using EcommerceAPI.Application.Interfaces;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Infrastructure.Persistence;
using EcommerceAPI.Infrastructure.Persistence.Interceptors;
using EcommerceAPI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddScoped<AuditableEntityInterceptor>();
        
        // Register Repositories
        service.AddScoped<IUserRepository, UserRepository>();
        service.AddScoped<IProductRepository, ProductRepository>();
        service.AddScoped<ICategoryRepository, CategoryRepository>();
        service.AddScoped<ICartRepository, CartRepository>();

        service.AddDbContext<EcommerceDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(EcommerceDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null
                    );
                });
        });

        return service;
    }
}