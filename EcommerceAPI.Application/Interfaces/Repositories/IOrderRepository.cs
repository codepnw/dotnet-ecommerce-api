using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetOrderByIdAsync(Guid id);
    Task AddASync(Order order);
    Task SaveChangeAsync();
}