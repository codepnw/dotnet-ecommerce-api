using Asp.Versioning;
using EcommerceAPI.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/order")]
// TODO: Uncomment later
// [Authorize]
public class OrderController(IOrderService service) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var userId = MockUserId();

        var result = await service.CheckoutAsync(userId);

        if (!result.IsSuccess)
            return BadRequest(result.ErrorMessage);

        return Ok(result.Data);
    }

    private static Guid MockUserId()
    {
        return Guid.Parse("ae85f895-c8d4-4507-bf57-529dd966a1a9");
    }
}