namespace EcommerceAPI.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
            
        try
        {
            Log.Information("Checking database connection...");
                
            var context = services.GetRequiredService<EcommerceDbContext>();
                
            if (await context.Database.CanConnectAsync())
            {
                Log.Information("Database connection successfully");
                    
                Log.Information("Applying migrations...");
                await context.Database.MigrateAsync();
                Log.Information("Migrations applied successfully");
            }
            else
            {
                Log.Warning("Cannot connect to the database!");
                Log.Information("Attempting to create database...");
                await context.Database.EnsureCreatedAsync();
                Log.Information("Database created successfully");
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while migrating or initializing the database");
            throw;
        }
    }
}