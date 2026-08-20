using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Enums;
using FluentAssertions;

namespace EcommerceAPI.Tests.Entities;

public class OrderTests
{
    private static Order CreateOrder() => new Order
    {
        // Default Status Pending
        UserId = Guid.NewGuid(),
        TotalPrice = 500m
    };

    [Fact]
    public void Pending_ToPaid_Success()
    {
        var order = CreateOrder();
        var result = order.Pay();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void Pending_ToShip_Fail()
    {
        var order = CreateOrder();
        var result = order.Ship();

        result.IsSuccess.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Paid_ToShip_Success()
    {
        var order = CreateOrder();
        order.Pay();
        
        var result = order.Ship();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public void Paid_ToCancel_Success()
    {
        var order = CreateOrder();
        order.Pay();

        var result = order.Cancel();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Paid_ToComplete_Fail()
    {
        var order = CreateOrder();
        order.Pay();

        var result = order.Complete();

        result.IsSuccess.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public void Shipped_ToComplete_Success()
    {
        var order = CreateOrder();
        order.Pay();
        order.Ship();

        var result = order.Complete();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Completed);
        order.ShippedAt.Should().NotBeNull();
    }

    [Fact]
    public void Completed_toCancel_Fail()
    {
        var order = CreateOrder();
        order.Pay();
        order.Ship();
        order.Complete();

        var result = order.Cancel();

        result.IsSuccess.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Completed);
    }
}