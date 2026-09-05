using EcommerceAPI.Application.DTOs.Orders;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Application.Models;
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
    public async Task<Result<PagedResult<OrderResponse>>> GetAllOrdersForAdminAsync(OrderQueryParams query)
    {
        var userId = currentUserService.UserId;

        if (!IsAdminOnly(userId))
            return Result<PagedResult<OrderResponse>>.Failure("Admin Only", ErrorCode.Forbidden);

        var response = await FindAllOrders(query, null);

        return Result<PagedResult<OrderResponse>>.Success(response);
    }

    public async Task<Result<PagedResult<OrderResponse>>> GetAllOrdersForOwnerAsync(OrderQueryParams query)
    {
        var userId = currentUserService.UserId;

        var response = await FindAllOrders(query, userId);

        return Result<PagedResult<OrderResponse>>.Success(response);
    }

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
        if (!IsOwnerOrAdmin(order.UserId))
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

    public async Task<Result> PayOrderAsync(Guid orderId)
    {
        var order = await orderRepository.GetOrderByIdAsync(orderId);

        if (order is null)
            return Result.Failure("Order not found", ErrorCode.NotFound);

        // Check User Permissions
        if (!IsOwnerOrAdmin(order.UserId))
            return Result.Failure("Cannot pay order: no permissions", ErrorCode.Forbidden);

        // Update order status
        var result = order.Pay();

        if (!result.IsSuccess)
            return Result.Failure(result.ErrorMessage!, result.ErrorCode);

        // Save DB
        await orderRepository.SaveChangeAsync();

        return Result.Success();
    }

    public async Task<Result> ShipOrderAsync(Guid orderId)
    {
        var order = await orderRepository.GetOrderByIdAsync(orderId);

        if (order is null)
            return Result.Failure("Order not found", ErrorCode.NotFound);

        // Check Admin Permissions
        if (!IsAdminOnly(order.UserId))
            return Result.Failure("Cannot ship order: admin only", ErrorCode.Forbidden);

        // Update order status
        var result = order.Ship();

        if (!result.IsSuccess)
            return Result.Failure(result.ErrorMessage!, result.ErrorCode);

        // Save DB
        await orderRepository.SaveChangeAsync();

        return Result.Success();
    }

    public async Task<Result> CompleteOrderAsync(Guid orderId)
    {
        var order = await orderRepository.GetOrderByIdAsync(orderId);

        if (order is null)
            return Result.Failure("Order not found", ErrorCode.NotFound);

        // Check Admin Permissions
        if (!IsAdminOnly(order.UserId))
            return Result.Failure("Cannot complete order: admin only", ErrorCode.Forbidden);

        // Update order status
        var result = order.Complete();

        if (!result.IsSuccess)
            return Result.Failure(result.ErrorMessage!, result.ErrorCode);

        // Save DB
        await orderRepository.SaveChangeAsync();

        return Result.Success();
    }

    private bool IsOwnerOrAdmin(Guid ownerId)
    {
        var userId = currentUserService.UserId;
        var userRole = currentUserService.Role;
        return ownerId == userId || userRole == UserRoles.Admin;
    }

    private bool IsAdminOnly(Guid ownerId)
    {
        var userRole = currentUserService.Role;
        return userRole == UserRoles.Admin;
    }

    private async Task<PagedResult<OrderResponse>> FindAllOrders(OrderQueryParams query, Guid? userId)
    {
        var result = await orderRepository.GetAllOrdersAsync(query, userId);

        var orders = result.Items.Select(o => new OrderResponse
        {
            Id = o.Id,
            TotalPrice = o.TotalPrice,
            Status = o.Status.ToString(),

            Items = o.Items.Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductNameAtPurchase,
                Price = i.PriceAtPurchase,
                Quantity = i.Quantity
            }).ToList()
        }).ToList();

        return new PagedResult<OrderResponse>
        {
            Items = orders,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount
        };
    }
}