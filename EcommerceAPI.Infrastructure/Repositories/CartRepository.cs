using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Infrastructure.Repositories;

public class CartRepository(EcommerceDbContext context) : ICartRepository
{
    public async Task<Cart?> GetCartByUserIdAsync(Guid userId)
    {
        return await context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Inventory)
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task AddAsync(Cart cart)
    {
        await context.AddAsync(cart);
    }

    public async Task AddCartItemAsync(CartItem item)
    {
        await context.CartItems.AddAsync(item);
    }

    public async Task SaveChangeAsync()
    {
        /*
        // ==========================================
        // 🕵️‍♂️ DEBUG: EF Core UPDATE/INSERT
        // ==========================================
        var changedEntries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Added || e.State == EntityState.Deleted)
            .ToList();

        Console.WriteLine("========== [REPO DEBUG] Before SaveChanges ==========");
        foreach (var entry in changedEntries)
        {
            Console.WriteLine($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
        
            foreach (var prop in entry.Properties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    Console.WriteLine($"  ➔ PK [{prop.EntityEntry}]: Current = {prop.CurrentValue} | Original = {prop.OriginalValue}");
                }
            }
        }
        Console.WriteLine("=====================================================");
        */
        
        await context.SaveChangesAsync();
    }
}