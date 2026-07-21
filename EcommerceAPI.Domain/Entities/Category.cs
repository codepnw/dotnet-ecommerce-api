using EcommerceAPI.Domain.Common;

namespace EcommerceAPI.Domain.Entities;

public class Category : BaseAuditableEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; } // SEO URL /categories/electronics
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
}