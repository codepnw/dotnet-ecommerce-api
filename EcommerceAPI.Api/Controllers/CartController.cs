using EcommerceAPI.Application.DTOs.Carts;
using EcommerceAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers;

[ApiController]
[Route("api/cart")]
// TODO: Uncomment later
// [Authorize]
public class CartController(ICartService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCart()
    {
        var userId = MockUserId();

        var result = await service.GetCartAsync(userId);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok(result.Data);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart(AddToCartRequest request)
    {
        var userId = MockUserId();

        var result = await service.AddItemAsync(userId, request);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateQuantity(UpdateCartRequest request)
    {
        var userId = MockUserId();

        var result = await service.UpdateItemQuantityAsync(userId, request);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpPost("remove")]
    public async Task<IActionResult> RemoveItem(RemoveCartItemRequest request)
    {
        var userId = MockUserId();

        var result = await service.RemoveItemAsync(userId, request);
        
        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpGet("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = MockUserId();

        var result = await service.ClearCartAsync(userId);
        
        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    private static Guid MockUserId()
    {
        return Guid.Parse("ae85f895-c8d4-4507-bf57-529dd966a1a9");
    }
}