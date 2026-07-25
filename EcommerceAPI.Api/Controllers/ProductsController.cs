using EcommerceAPI.Application.DTOs.Products;
using EcommerceAPI.Application.Services;
using EcommerceAPI.Domain.Shared;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
public class ProductsController(ProductService productService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var result = await productService.CreateProductAsync(request);

        if (!result.IsSuccess)
        {
            return result.ErrorCode switch
            {
                ErrorCode.Conflict => Conflict(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        return CreatedAtAction(nameof(GetProductById), new { id = result.Data!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProducts()
    {
        var result = await productService.GetAllProductsAsync();

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var result = await productService.GetProductByIdAsync(id);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok(result.Data);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var result = await productService.UpdateProductAsync(id, request);
        
        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await productService.DeleteProductAsync(id);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return NoContent();
    }
}