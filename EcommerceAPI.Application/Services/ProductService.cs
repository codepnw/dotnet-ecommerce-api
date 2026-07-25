using EcommerceAPI.Application.DTOs.Products;
using EcommerceAPI.Application.Interfaces;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Application.Mappings;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Services;

public class ProductService(IProductRepository productRepository) : IProductService
{
    public async Task<Result<ProductResponse>> CreateProductAsync(CreateProductRequest request)
    {
        if (await productRepository.IsSkuExistsAsync(request.Sku))
            return Result<ProductResponse>.Failure("SKU already exists", ErrorCode.Conflict);

        // Save to Database
        var createdProduct = await productRepository.CreateAsync(request.ToEntity());

        var response = createdProduct.ToResponse();

        return Result<ProductResponse>.Success(response);
    }

    public async Task<Result<List<ProductResponse>>> GetAllProductsAsync()
    {
        var products = await productRepository.GetAllAsync();

        var responseList = products.Select(p => p.ToResponse()).ToList();

        return Result<List<ProductResponse>>.Success(responseList);
    }

    public async Task<Result<ProductResponse>> GetProductByIdAsync(Guid id)
    {
        var product = await productRepository.GetByIdAsync(id);

        if (product is null)
            return Result<ProductResponse>.Failure("Product not found", ErrorCode.NotFound);

        var response = product.ToResponse();

        return Result<ProductResponse>.Success(response);
    }

    public async Task<Result> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var product = await productRepository.GetByIdAsync(id, true);

        if (product is null)
            return Result.Failure("Product not found", ErrorCode.NotFound);

        request.ApplyTo(product);

        // Save to Database
        await productRepository.UpdateAsync(product);

        return Result.Success();
    }

    public async Task<Result> DeleteProductAsync(Guid id)
    {
        var isDeleted = await productRepository.SoftDeleteAsync(id);
        
        return isDeleted 
            ? Result.Success()
            : Result.Failure("Product not found", ErrorCode.NotFound);
    }
}