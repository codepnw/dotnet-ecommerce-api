namespace EcommerceAPI.Application.DTOs.Carts;

public class CartResponse
{
    public Guid Id { get; set; }
    public List<CartItemResponse> Items { get; set; } = [];
    public decimal TotalPrice { get; set; }
}

public class CartItemResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}