using Asp.Versioning;
using EcommerceAPI.Application.DTOs.Carts;
using EcommerceAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cart")]
[Authorize]
public class CartController(ICartService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var result = await service.GetCartAsync();

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok(result.Data);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart(AddToCartRequest request)
    {
        var result = await service.AddItemAsync(request);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateQuantity(UpdateCartRequest request)
    {
        var result = await service.UpdateItemQuantityAsync(request);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpPost("remove")]
    public async Task<IActionResult> RemoveItem(RemoveCartItemRequest request)
    {
        var result = await service.RemoveItemAsync(request);
        
        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpGet("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var result = await service.ClearCartAsync();
        
        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }
}