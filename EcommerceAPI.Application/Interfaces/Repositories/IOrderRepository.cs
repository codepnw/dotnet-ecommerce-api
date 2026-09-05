using EcommerceAPI.Application.Models;
using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetAllOrdersAsync(OrderQueryParams query, Guid? userId);
    Task<Order?> GetOrderByIdAsync(Guid id);
    Task AddASync(Order order);
    Task SaveChangeAsync();
}