using System.Security.Cryptography.X509Certificates;
using EcommerceAPI.Application.DTOs.Products;
using EcommerceAPI.Domain.Common.Extensions;
using EcommerceAPI.Domain.Common.ValueObject;
using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.Mappings;

public static class ProductMappingExtensions
{
    public static ProductResponse ToResponse(this Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Slug = product.Slug,
            PriceAmount = product.Price.Amount,
            PriceCurrency = product.Price.Currency,
            AvailableQuantity = product.Inventory.AvailableQuantity
        };
    }

    public static Product ToEntity(this CreateProductRequest request)
    {
        return new Product
        {
            Name = request.Name,
            Sku = request.Sku,
            Slug = request.Name.GenerateSlug(),
            Price = Money.Create(request.PriceAmount, request.PriceCurrency),
            CategoryId = request.CategoryId,

            Inventory = new ProductInventory
            {
                QuantityOnHand = 0,
                QuantityReserved = 0
            }
        };
    }

    public static void ApplyTo(this UpdateProductRequest request, Product product)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            product.Name = request.Name;
            product.Slug = request.Name.GenerateSlug();
        }
        else
        {
            product.Name = product.Name;
        }
        
        product.CategoryId = request.CategoryId != Guid.Empty ? request.CategoryId : product.CategoryId;

        if (request.PriceAmount <= 0) return;
        
        var currency = !string.IsNullOrWhiteSpace(request.PriceCurrency)
            ? request.PriceCurrency
            : product.Price.Currency;

        product.Price = Money.Create(request.PriceAmount, currency);
    } 
}