using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Models;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Enums;
using EcommerceAPI.Domain.Shared;
using EcommerceAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EcommerceAPI.Infrastructure.Repositories;

public class OrderRepository(EcommerceDbContext context) : IOrderRepository
{
    public async Task<PagedResult<Order>> GetAllOrdersAsync(OrderQueryParams query, Guid? userId)
    {
        IQueryable<Order> orders = context.Orders.Include(o => o.Items).AsQueryable();

        // Filter User
        if (userId.HasValue)
            orders = orders.Where(o => o.UserId == userId.Value);

        // Filter Status
        if (query.Status.HasValue)
            orders = orders.Where(o => o.Status == query.Status.Value);

        // Sort
        orders = query.SortBy.ToLower() switch
        {
            "totalPrice" => query.Descending ? orders.OrderByDescending(o => o.TotalPrice) : orders.OrderBy(o => o.TotalPrice),

            "createdAt" => query.Descending ? orders.OrderByDescending(o => o.CreatedAt) : orders.OrderBy(o => o.CreatedAt),

            _ => orders.OrderByDescending(o => o.CreatedAt)
        };

        var totalCount = await orders.CountAsync();

        var items = await orders
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<Order>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

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