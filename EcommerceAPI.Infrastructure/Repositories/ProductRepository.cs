using EcommerceAPI.Application.Interfaces;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Infrastructure.Repositories;

public class ProductRepository(EcommerceDbContext context) : IProductRepository
{
    public async Task<Product> CreateAsync(Product product)
    {
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();
        return product;
    }

    public async Task<Product?> GetByIdAsync(Guid id, bool track = false)
    {
        var query = context.Products.Include(p => p.Inventory).AsQueryable();

        if (!track)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetAllAsync()
    {
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Inventory)
            .ToListAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        context.Products.Update(product);
        await context.SaveChangesAsync();
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var product = await context.Products.FindAsync(id);

        if (product is null) return false;

        product.IsDeleted = true;

        await context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> IsSkuExistsAsync(string sku)
    {
        return await context.Products.AnyAsync(p => p.Sku == sku);
    }
}