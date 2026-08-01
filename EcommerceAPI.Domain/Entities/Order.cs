using EcommerceAPI.Domain.Common;
using EcommerceAPI.Domain.Enums;

namespace EcommerceAPI.Domain.Entities;

public class Order : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public decimal TotalPrice { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public ICollection<OrderItem> Items { get; set; } = [];
}