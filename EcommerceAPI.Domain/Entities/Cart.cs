using EcommerceAPI.Domain.Common;

namespace EcommerceAPI.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<CartItem> Items { get; set; } = [];
    
    // ----------- Business Logic ------------

    public void AddItem(Guid productId, int quantity)
    {
        // Check item in cart
        var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem is not null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            Items.Add(new CartItem
            {
                CartId = Id,
                ProductId = productId,
                Quantity = quantity
            });
        }
    }

    public void RemoveItem(Guid productId)
    {
        var item = Items.FirstOrDefault(i => i.ProductId == productId);

        if (item is not null)
            Items.Remove(item);
    }
}