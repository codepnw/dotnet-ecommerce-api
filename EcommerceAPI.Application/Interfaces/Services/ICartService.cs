using EcommerceAPI.Application.DTOs.Carts;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Application.Interfaces.Services;

public interface ICartService
{
    Task<Result<CartResponse>> GetCartAsync();
    Task<Result> AddItemAsync(AddToCartRequest request);
    Task<Result> UpdateItemQuantityAsync(UpdateCartRequest request);
    Task<Result> RemoveItemAsync(RemoveCartItemRequest request);
    Task<Result> ClearCartAsync();
}