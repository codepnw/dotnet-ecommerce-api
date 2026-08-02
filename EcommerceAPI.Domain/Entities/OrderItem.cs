using EcommerceAPI.Domain.Common;

namespace EcommerceAPI.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public string ProductNameAtPurchase { get; set; } = string.Empty;
}