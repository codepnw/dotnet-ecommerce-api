using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.DTOs.Orders;

public class OrderItemResponse
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}