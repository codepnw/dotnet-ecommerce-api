using EcommerceAPI.Application.DTOs.Products;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Interfaces.Services;

public interface IProductService
{
    Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request);
    Task<Result<List<ProductResponse>>> GetAllProductsAsync();
    Task<Result<ProductResponse>> GetProductByIdAsync(Guid id);
    Task<Result> UpdateProductAsync(Guid id, UpdateProductRequest request);
    Task<Result> DeleteProductAsync(Guid id);
}