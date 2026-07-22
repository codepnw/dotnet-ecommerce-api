using EcommerceAPI.Application.Interfaces;
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

        service.AddDbContext<EcommerceDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
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