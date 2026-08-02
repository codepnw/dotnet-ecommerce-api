namespace EcommerceAPI.Application.DTOs.Carts;

public class AddToCartRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}