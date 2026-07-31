namespace EcommerceAPI.Application.DTOs.Carts;

public class UpdateCartRequest
{
    public Guid ProductId { get; set; }
    public int NewQuantity { get; set; }
}