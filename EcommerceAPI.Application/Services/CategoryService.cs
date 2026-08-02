using EcommerceAPI.Application.DTOs.Categories;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Application.Mappings;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Services;

public class CategoryService(ICategoryRepository categoryRepository) : ICategoryService
{
    public async Task<Result<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        if (await categoryRepository.IsNameExistsAsync(request.Name))
            return Result<CategoryResponse>.Failure("Name already exists", ErrorCode.Conflict);

        // Save to Database
        var created = await categoryRepository.CreateAsync(request.ToEntity());

        var response = created.ToResponse();

        return Result<CategoryResponse>.Success(response);
    }

    public async Task<Result<List<CategoryResponse>>> GetAllCategoriesAsync()
    {
        var categories = await categoryRepository.GetAllAsync();

        var response = categories.Select(c => c.ToResponse()).ToList();

        return Result<List<CategoryResponse>>.Success(response);
    }

    public async Task<Result<CategoryResponse>> GetCategoryByIdAsync(Guid id)
    {
        var category = await categoryRepository.GetByIdAsync(id);

        return category is null
            ? Result<CategoryResponse>.Failure("Category not found", ErrorCode.NotFound)
            : Result<CategoryResponse>.Success(category.ToResponse());
    }

    public async Task<Result> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request)
    {
        // Find by Id
        var category = await categoryRepository.GetByIdAsync(id, true);

        if (category is null)
            return Result.Failure("Category not found", ErrorCode.NotFound);

        // Check name exists
        if (await categoryRepository.IsNameExistsAsync(request.Name))
            return Result.Failure("Category already exists", ErrorCode.Conflict);

        request.ApplyTo(category);

        // Save to Database
        await categoryRepository.UpdateAsync(category);

        return Result.Success();
    }

    public async Task<Result> DeleteCategoryAsync(Guid id)
    {
        var isDeleted = await categoryRepository.SoftDeleteAsync(id);

        return isDeleted
            ? Result.Success()
            : Result.Failure("Category not found", ErrorCode.NotFound);
    }
}