using EcommerceAPI.Application.DTOs.Orders;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Interfaces.Services;

public interface IOrderService
{
    Task<Result<OrderResponse>> CheckoutAsync(Guid userId);
}