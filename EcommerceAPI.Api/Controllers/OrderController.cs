using Asp.Versioning;
using EcommerceAPI.Application.Interfaces.Services;
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
}