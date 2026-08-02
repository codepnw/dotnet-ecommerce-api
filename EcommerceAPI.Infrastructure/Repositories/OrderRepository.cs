using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Infrastructure.Persistence;

namespace EcommerceAPI.Infrastructure.Repositories;

public class OrderRepository(EcommerceDbContext context) : IOrderRepository
{
    public async Task AddASync(Order order)
    {
        await context.AddAsync(order);
    }

    public async Task SaveChangeAsync()
    {
        await context.SaveChangesAsync();
    }
}