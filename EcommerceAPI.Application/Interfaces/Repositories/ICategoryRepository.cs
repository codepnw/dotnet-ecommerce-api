using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<Category> CreateAsync(Category category);
    Task<List<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(Guid id, bool track = false);
    Task UpdateAsync(Category category);
    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> IsNameExistsAsync(string name);
}