using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.DTOs.Orders;

public class OrderResponse
{
    public Guid Id { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<OrderItemResponse> Items { get; set; } = [];
}