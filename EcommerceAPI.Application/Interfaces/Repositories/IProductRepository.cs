using EcommerceAPI.Application.Models;
using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product> CreateAsync(Product product);
    Task<Product?> GetByIdAsync(Guid id, bool track = false);
    Task<PagedResult<Product>> GetAllAsync(ProductQueryParams query);
    Task<bool> IsSkuExistsAsync(string sku);
    Task UpdateAsync(Product product);
    Task<bool> SoftDeleteAsync(Guid id);
}