using EcommerceAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.Infrastructure.Persistence.Configurations;

public class ProductInventoryConfiguration : IEntityTypeConfiguration<ProductInventory>
{
    public void Configure(EntityTypeBuilder<ProductInventory> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.QuantityOnHand).IsRequired();
        builder.Property(i => i.QuantityReserved).IsRequired();

        builder.Ignore(i => i.AvailableQuantity);
    }
}