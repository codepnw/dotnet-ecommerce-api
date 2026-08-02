namespace EcommerceAPI.Application.DTOs.Products;

public class UpdateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string PriceCurrency { get; set; } = "THB";
    public Guid CategoryId { get; set; }
}