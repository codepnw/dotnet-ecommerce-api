using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceAPI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        // Register Services
        service.AddScoped<IAuthService, AuthService>();
        service.AddScoped<IOAuthService, OAuthService>();
        service.AddScoped<IProductService, ProductService>();
        
        return service;
    }
}