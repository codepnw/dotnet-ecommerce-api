using System.Diagnostics;
using Asp.Versioning;
using EcommerceAPI.Application.DTOs.Categories;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/categories")]
// TODO: Uncomment later
public class CategoryController(ICategoryService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var result = await service.CreateCategoryAsync(request);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.Conflict => Conflict(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        return CreatedAtAction(
            nameof(GetCategoryById),
            new { id = result.Data!.Id },
            result.Data
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
        var result = await service.GetAllCategoriesAsync();

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var result = await service.GetCategoryByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Data)
            : BadRequest(result.ErrorMessage);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await service.UpdateCategoryAsync(id, request);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.ErrorMessage);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var result = await service.DeleteCategoryAsync(id);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.ErrorMessage);
    }
}