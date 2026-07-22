using EcommerceAPI.Domain.Shared;
using BaseAuditableEntity = EcommerceAPI.Domain.Common.BaseAuditableEntity;

namespace EcommerceAPI.Domain.Entities;

public class ProductInventory : BaseAuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // Real Quantity
    public int QuantityOnHand { get; set; }
    // Quantity Reserved (Pending Checkout) : Prevent Overselling
    public int QuantityReserved { get; set; }
    
    // Calculate Product Quantity Available
    public int AvailableQuantity => QuantityOnHand - QuantityReserved;
    
    // Method Business Logic (Encapsulation in DDD)
    public Result ReserveStock(int quantity)
    {
        if (AvailableQuantity < quantity)
            return Result.Failure("Insufficient stock available", ErrorCode.Conflict);

        QuantityReserved += quantity;
        return Result.Success();
    }

    public void ReleaseStock(int quantity) => QuantityReserved -= quantity;
}