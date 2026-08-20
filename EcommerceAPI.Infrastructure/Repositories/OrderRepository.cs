using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Infrastructure.Repositories;

public class OrderRepository(EcommerceDbContext context) : IOrderRepository
{
    public async Task<Order?> GetOrderByIdAsync(Guid id)
    {
        return await context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Inventory)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task AddASync(Order order)
    {
        await context.AddAsync(order);
    }

    public async Task SaveChangeAsync()
    {
        await context.SaveChangesAsync();
    }
}