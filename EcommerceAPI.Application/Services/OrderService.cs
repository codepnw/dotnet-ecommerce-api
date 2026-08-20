using EcommerceAPI.Application.DTOs.Orders;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Enums;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Services;

public class OrderService(
    IOrderRepository orderRepository,
    ICartRepository cartRepository,
    ICurrentUserService currentUserService
) : IOrderService
{
    public async Task<Result<OrderResponse>> CheckoutAsync()
    {
        var userId = currentUserService.UserId;

        var cart = await cartRepository.GetCartByUserIdAsync(userId);

        if (cart is null || cart.Items.Count == 0)
            return Result<OrderResponse>.Failure("Cart is empty", ErrorCode.BadRequest);

        decimal totalPrice = 0;
        List<OrderItem> orderItems = [];

        foreach (var cartItem in cart.Items)
        {
            var inventory = cartItem.Product.Inventory;

            // Calculate Price
            var subTotal = cartItem.Product.Price.Amount * cartItem.Quantity;
            totalPrice += subTotal;

            // Add orderItem to List
            var orderItem = new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                PriceAtPurchase = cartItem.Product.Price.Amount,
                ProductNameAtPurchase = cartItem.Product.Name
            };
            orderItems.Add(orderItem);

            // Decrease Quantity & Reserved Quantity
            var result = inventory.ConfirmSale(cartItem.Quantity);

            if (!result.IsSuccess)
                return Result<OrderResponse>.Failure(result.ErrorMessage!, result.ErrorCode);
        }

        var order = new Order
        {
            UserId = userId,
            TotalPrice = totalPrice,
            Items = orderItems
        };

        // Clear Cart
        cart.Items.Clear();

        // Save to Database
        await orderRepository.AddASync(order);
        await orderRepository.SaveChangeAsync();

        var response = new OrderResponse
        {
            Id = order.Id,
            TotalPrice = totalPrice,
            Status = order.Status.ToString(),
            Items = orderItems.Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductNameAtPurchase,
                Quantity = i.Quantity,
                Price = i.PriceAtPurchase
            }).ToList()
        };

        return Result<OrderResponse>.Success(response);
    }

    public async Task<Result> CancelOrderAsync(Guid orderId)
    {
        // Get Order
        var order = await orderRepository.GetOrderByIdAsync(orderId);

        if (order is null)
            return Result.Failure("Order not found", ErrorCode.NotFound);

        // Check User Permissions
        var userId = currentUserService.UserId;
        var userRole = currentUserService.Role;

        if (order.UserId != userId && userRole != UserRoles.Admin)
            return Result.Failure("Cannot cancel order: no permissions", ErrorCode.Forbidden);

        // Update Order Status to Cancel
        var result = order.Cancel();
        
        if (!result.IsSuccess)
            return Result.Failure(result.ErrorMessage!, result.ErrorCode);

        // Return Product Stock
        foreach (var item in order.Items)
        {
            var stockResult = item.Product.Inventory.ReturnStock(item.Quantity);

            if (!stockResult.IsSuccess)
                return Result.Failure(stockResult.ErrorMessage!, stockResult.ErrorCode);
        }

        // Save Db
        await orderRepository.SaveChangeAsync();

        return Result.Success();
    }
}