using EcommerceAPI.Application.DTOs.Categories;
using EcommerceAPI.Domain.Common.Extensions;
using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.Mappings;

public static class CategoryMappingExtensions
{
    public static CategoryResponse ToResponse(this Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description!
        };
    }

    public static Category ToEntity(this CreateCategoryRequest request)
    {
        return new Category
        {
            Name = request.Name,
            Slug = request.Name.GenerateSlug(),
            Description = request.Description
        };
    }

    public static void ApplyTo(this UpdateCategoryRequest request, Category category)
    {
        category.Name = !string.IsNullOrWhiteSpace(request.Name) 
            ? request.Name : category.Name;

        category.Description = !string.IsNullOrWhiteSpace(request.Description) 
            ? request.Description : category.Description;
    }
}