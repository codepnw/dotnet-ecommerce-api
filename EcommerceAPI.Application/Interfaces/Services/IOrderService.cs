using EcommerceAPI.Application.DTOs.Orders;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Interfaces.Services;

public interface IOrderService
{
    Task<Result<OrderResponse>> CheckoutAsync();
    Task<Result> CancelOrderAsync(Guid orderId);
    Task<Result> PayOrderAsync(Guid orderId);
    Task<Result> ShipOrderAsync(Guid orderId);
    Task<Result> CompleteOrderAsync(Guid orderId);
}