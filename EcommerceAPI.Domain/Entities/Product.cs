using EcommerceAPI.Domain.Common;
using EcommerceAPI.Domain.Common.ValueObject;

namespace EcommerceAPI.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public required string Sku { get; set; }
    public Money Price { get; private set; } = Money.Zero();
    
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Navigation Property (1:1)
    public ProductInventory Inventory { get; set; } = null!;

    // Method for Update Price
    public void UpdatePrice(Money newPrice) => Price = newPrice;
}