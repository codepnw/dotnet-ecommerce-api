using EcommerceAPI.Application.DTOs.Carts;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Interfaces.Services;

public interface ICartService
{
    Task<Result<CartResponse>> GetCartAsync(Guid userId);
    Task<Result> AddItemAsync(Guid userId, AddToCartRequest request);
    Task<Result> UpdateItemQuantityAsync(Guid userId, UpdateCartRequest request);
    Task<Result> RemoveItemAsync(Guid userId, RemoveCartItemRequest request);
    Task<Result> ClearCartAsync(Guid userId);
}