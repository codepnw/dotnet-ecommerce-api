using EcommerceAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EcommerceAPI.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(255);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);

        builder.Property(u => u.Role).IsRequired().HasMaxLength(30).HasDefaultValue("User");

        builder.Property(u => u.RefreshToken).HasMaxLength(512);

        // OAuth Google
        builder.Property(u => u.GoogleId).IsUnicode().HasMaxLength(100);
        builder.HasIndex(u => u.GoogleId).IsUnique().HasFilter("[GoogleId] IS NOT NULL");
        builder.Property(u => u.PictureUrl).HasMaxLength(1000);
        builder.Property(u => u.DisplayName).HasMaxLength(100);

        // 1 : M
        builder.HasMany(u => u.Orders).WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}