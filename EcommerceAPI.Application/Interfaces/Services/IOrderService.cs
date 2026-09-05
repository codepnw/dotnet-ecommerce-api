using EcommerceAPI.Application.DTOs.Orders;
using EcommerceAPI.Application.Models;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Interfaces.Services;

public interface IOrderService
{
    Task<Result<PagedResult<OrderResponse>>> GetAllOrdersForAdminAsync(OrderQueryParams query);
    Task<Result<PagedResult<OrderResponse>>> GetAllOrdersForOwnerAsync(OrderQueryParams query);
    Task<Result<OrderResponse>> CheckoutAsync();
    Task<Result> CancelOrderAsync(Guid orderId);
    Task<Result> PayOrderAsync(Guid orderId);
    Task<Result> ShipOrderAsync(Guid orderId);
    Task<Result> CompleteOrderAsync(Guid orderId);
}