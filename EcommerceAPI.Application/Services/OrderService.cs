using EcommerceAPI.Application.DTOs.Orders;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Enums;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Services;

public class OrderService(IOrderRepository orderRepository, ICartRepository cartRepository) : IOrderService
{
    public async Task<Result<OrderResponse>> CheckoutAsync(Guid userId)
    {
        var cart = await cartRepository.GetCartByUserIdAsync(userId);

        if (cart is null || cart.Items.Count == 0)
            return Result<OrderResponse>.Failure("Cart is empty", ErrorCode.BadRequest);

        decimal totalPrice = 0;
        List<OrderItem> orderItems = [];

        foreach (var cartItem in cart.Items)
        {
            var inventory = cartItem.Product.Inventory;

            if (inventory.AvailableQuantity < cartItem.Quantity)
                return Result<OrderResponse>.Failure("Product out of stock", ErrorCode.Conflict);

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
            inventory.ConfirmSale(cartItem.Quantity);
        }

        var order = new Order
        {
            UserId = userId,
            TotalPrice = totalPrice,
            Status = OrderStatus.Pending,
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
}