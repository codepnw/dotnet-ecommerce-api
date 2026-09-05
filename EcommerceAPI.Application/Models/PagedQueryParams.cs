using EcommerceAPI.Domain.Enums;

namespace EcommerceAPI.Application.Models;

public record PagedQueryParams
{
    private const int MaxPageSize = 50;
    public int PageNumber { get; set; } = 1;
    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string SortBy { get; init; } = "createdAt";
    public bool Descending { get; init; } = true;
}

public record ProductQueryParams : PagedQueryParams
{
    public string? SearchName { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
}

public record OrderQueryParams : PagedQueryParams
{
    public OrderStatus? Status { get; init; }
}