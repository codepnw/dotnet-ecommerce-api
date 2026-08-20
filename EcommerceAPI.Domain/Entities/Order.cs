using EcommerceAPI.Domain.Common;
using EcommerceAPI.Domain.Enums;
using EcommerceAPI.Domain.Shared;

namespace EcommerceAPI.Domain.Entities;

public class Order : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    public ICollection<OrderItem> Items { get; set; } = [];

    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? ShippedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancelledReason { get; private set; }

    // -------------- Logic ----------------
    
    public Result Cancel(string? reason = null)
    {
        if (Status != OrderStatus.Pending && Status != OrderStatus.Paid)
            return Result.Failure("Status Cancel for Pending and Paid", ErrorCode.Conflict);

        Status = OrderStatus.Cancelled;

        CancelledAt = DateTimeOffset.UtcNow;
        CancelledReason = reason;

        return Result.Success();
    }

    public Result Pay()
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure("Status Paid for Pending only", ErrorCode.Conflict);

        Status = OrderStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    public Result Ship()
    {
        if (Status != OrderStatus.Paid)
            return Result.Failure("Status Shipped for Paid only", ErrorCode.Conflict);

        Status = OrderStatus.Shipped;
        ShippedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    public Result Complete()
    {
        if (Status != OrderStatus.Shipped)
            return Result.Failure("Status Completed for Shipped only", ErrorCode.Conflict);

        Status = OrderStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}