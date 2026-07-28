using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Infrastructure.Repositories;

public class CategoryRepository(EcommerceDbContext context) : ICategoryRepository
{
    public async Task<Category> CreateAsync(Category category)
    {
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();
        return category;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        return await context.Categories.AsNoTracking().ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id, bool track = false)
    {
        var query = context.Categories.AsQueryable();

        if (!track)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task UpdateAsync(Category category)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync();
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var category = await context.Categories.FindAsync(id);

        if (category is null) return false;

        category.IsDeleted = true;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsNameExistsAsync(string name)
    {
        return await context.Categories.AnyAsync(c => c.Name == name);
    }
}