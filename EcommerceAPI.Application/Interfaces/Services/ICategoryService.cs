using EcommerceAPI.Application.DTOs.Categories;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Interfaces.Services;

public interface ICategoryService
{
    Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<Result<List<CategoryResponse>>> GetAllCategoriesAsync();
    Task<Result<CategoryResponse>> GetCategoryByIdAsync(Guid id);
    Task<Result> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request);
    Task<Result> DeleteCategoryAsync(Guid id);
}