using EcommerceAPI.Application.DTOs.Carts;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Services;

public class CartService(ICartRepository cartRepository, IProductRepository productRepository) : ICartService
{
    public async Task<Result<CartResponse>> GetCartAsync(Guid userId)
    {
        var cart = await cartRepository.GetCartByUserIdAsync(userId);

        if (cart is null)
            return Result<CartResponse>.Success(new CartResponse());

        List<CartItemResponse> items = [];
        decimal totalPrice = 0;

        // Add Product to List & Calculate TotalPrice
        foreach (var item in cart.Items)
        {
            var subTotal = item.Product.Price.Amount * item.Quantity;

            items.Add(new CartItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                Price = item.Product.Price.Amount,
                Quantity = item.Quantity,
                Subtotal = subTotal
            });

            totalPrice += subTotal;
        }

        var response = new CartResponse
        {
            Id = cart.Id,
            Items = items,
            TotalPrice = totalPrice
        };

        return Result<CartResponse>.Success(response);
    }

    public async Task<Result> AddItemAsync(Guid userId, AddToCartRequest request)
    {
        // TODO: Still has Bug
        
        // Get Cart
        var cart = await cartRepository.GetCartByUserIdAsync(userId);

        if (cart is null)
        {
            cart = new Cart { UserId = userId };
            await cartRepository.AddAsync(cart);
        }

        var cartItems = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        Product? product;

        if (cartItems?.Product?.Inventory != null)
        {
            product = cartItems.Product;
        }
        else
        {
            product = await productRepository.GetByIdAsync(request.ProductId, true);
            if (product?.Inventory is null)
                return Result.Failure("Product or inventory not found", ErrorCode.NotFound);
        }

        // Check Stock Available
        if (product.Inventory.AvailableQuantity < request.Quantity)
            return Result.Failure("Insufficient stock available", ErrorCode.Conflict);
        
        // Reserve Stock
        var reserveResult = product.Inventory.ReserveStock(request.Quantity);

        if (!reserveResult.IsSuccess)
            return Result.Failure(reserveResult.ErrorMessage!, reserveResult.ErrorCode);

        Console.WriteLine($"Reserver Product: {reserveResult.IsSuccess}, {request.Quantity}");

        // Add to Cart
        cart.AddItem(request.ProductId, request.Quantity);

        try
        {
            // Save to Database
            await cartRepository.SaveChangeAsync();
            return Result.Success();
        }
        catch (Exception e)
        {
            return Result.Failure($"Exception: {e.Message}", ErrorCode.Conflict);
        }
    }

    public async Task<Result> UpdateItemQuantityAsync(Guid userId, UpdateCartRequest request)
    {
        if (request.NewQuantity <= 0)
            return await RemoveItemAsync(userId, new RemoveCartItemRequest { ProductId = request.ProductId });

        var cart = await cartRepository.GetCartByUserIdAsync(userId);
        var product = await productRepository.GetByIdAsync(request.ProductId, true);
        var inventory = product?.Inventory;

        var item = cart?.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (item is null || inventory is null)
            return Result.Failure("Cart is empty", ErrorCode.BadRequest);

        var diff = request.NewQuantity - item.Quantity;

        if (diff > 0)
        {
            // Reserve Stock
            var result = inventory.ReserveStock(diff);
            if (!result.IsSuccess)
                return Result.Failure(result.ErrorMessage!, result.ErrorCode);
        }
        else if (diff < 0)
        {
            // Release Stock
            inventory.ReleaseStock(Math.Abs(diff));
        }

        item.Quantity = request.NewQuantity;

        await cartRepository.SaveChangeAsync();

        return Result.Success();
    }

    public async Task<Result> RemoveItemAsync(Guid userId, RemoveCartItemRequest request)
    {
        var cart = await cartRepository.GetCartByUserIdAsync(userId);
        var product = await productRepository.GetByIdAsync(request.ProductId);
        var inventory = product?.Inventory;

        var item = cart?.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        if (item is null || inventory is null)
            return Result.Failure("Product is empty", ErrorCode.BadRequest);

        inventory.ReleaseStock(item.Quantity);

        cart?.Items.Remove(item);

        await cartRepository.SaveChangeAsync();

        return Result.Success();
    }

    public async Task<Result> ClearCartAsync(Guid userId)
    {
        var cart = await cartRepository.GetCartByUserIdAsync(userId);

        if (cart is null || cart.Items.Count == 0)
            return Result.Success();

        // Release Stock from Cart
        foreach (var item in cart.Items)
        {
            item.Product?.Inventory?.ReleaseStock(item.Quantity);
        }

        cart.Items.Clear();

        await cartRepository.SaveChangeAsync();

        return Result.Success();
    }
}