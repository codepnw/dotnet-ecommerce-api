using EcommerceAPI.Domain.Entities;

namespace EcommerceAPI.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task AddASync(Order order);
    Task SaveChangeAsync();
}