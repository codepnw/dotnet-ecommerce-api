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

    // ------------- Logic --------------
    
    public Result ReserveStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero", ErrorCode.BadRequest);
        
        if (AvailableQuantity < quantity)
            return Result.Failure("Insufficient stock available", ErrorCode.Conflict);

        QuantityReserved += quantity;
        return Result.Success();
    }

    public Result ReleaseStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero", ErrorCode.BadRequest);

        if (quantity > QuantityReserved)
            return Result.Failure("Cannot release more than reserved", ErrorCode.Conflict);

        QuantityReserved -= quantity;
        return Result.Success();
    }

    public Result ConfirmSale(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero", ErrorCode.BadRequest);

        if (quantity > QuantityReserved)
            return Result.Failure("Cannot confirm more than reserved", ErrorCode.Conflict);

        if (quantity > QuantityOnHand)
            return Result.Failure("Cannot confirm more than on hand", ErrorCode.Conflict);

        QuantityOnHand -= quantity;
        QuantityReserved -= quantity;
        return Result.Success();
    }

    public Result ReturnStock(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than zero", ErrorCode.BadRequest);

        QuantityOnHand += quantity;
        return Result.Success();
    }
}