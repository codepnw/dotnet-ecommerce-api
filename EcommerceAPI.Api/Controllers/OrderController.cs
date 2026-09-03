using Asp.Versioning;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/order")]
[Authorize]
public class OrderController(IOrderService service) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var result = await service.CheckoutAsync();

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok(result.Data);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await service.CancelOrderAsync(id);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(Guid id)
    {
        var result = await service.PayOrderAsync(id);

         if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpPost("{id:guid}/ship")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Ship(Guid id)
    {
        var result = await service.ShipOrderAsync(id);

         if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> Complete(Guid id)
    {
        var result = await service.CompleteOrderAsync(id);

         if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok();
    }
}