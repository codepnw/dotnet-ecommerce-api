using EcommerceAPI.Application.Interfaces;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Models;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Shared;
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

    public async Task<PagedResult<Product>> GetAllAsync(ProductQueryParams query)
    {
        IQueryable<Product> products = context.Products.Include(p => p.Inventory).AsQueryable();

        // Filter
        if (!string.IsNullOrWhiteSpace(query.SearchName))
            products = products.Where(p => p.Name.Contains(query.SearchName));

        if (query.MinPrice.HasValue)
            products = products.Where(p => p.Price.Amount >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            products = products.Where(p => p.Price.Amount <= query.MaxPrice.Value);

        products = query.SortBy.ToLower() switch
        {
            "price" => query.Descending ? products.OrderByDescending(p => p.Price.Amount) : products.OrderBy(p => p.Price.Amount),

            "name" => query.Descending ? products.OrderByDescending(p => p.Name) : products.OrderBy(p => p.Name),

            _ => products.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await products.CountAsync();

        var items = await products
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
          Items = items,
          TotalCount = totalCount,
          PageNumber = query.PageNumber,
          PageSize = query.PageSize  
        };
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