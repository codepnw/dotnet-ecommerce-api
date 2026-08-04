using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Services;
using EcommerceAPI.Domain.Common.ValueObject;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Shared;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new Mock<IOrderRepository>();
    private readonly Mock<ICartRepository> _cartRepo = new Mock<ICartRepository>();
    private readonly OrderService _serivce;

    public OrderServiceTests()
    {
        _serivce = new OrderService(_orderRepo.Object, _cartRepo.Object);
    }

    [Fact]
    public async Task Checkout_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const decimal price = 100m;
        const int quantity = 2;

        // (OnHand 10, Reserved 0 -> Available 10)
        var product = new Product
        {
            Name = "Test Product",
            Sku = "test-sku",
            Slug = "test-slug",
            Price = Money.Create(price),
            Inventory = new ProductInventory
            {
                QuantityOnHand = 10,
                QuantityReserved = 0
            }
        };
        var productId = product.Id;

        // Create Cart
        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = quantity,
                    Product = product // ⚠️ NOTE: If no Product cartItem.Product is null
                }
            }
        };

        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync(cart);
        _orderRepo.Setup(x => x.AddASync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(x => x.SaveChangeAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _serivce.CheckoutAsync(userId);

        // Assert 1
        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalPrice.Should().Be(price * quantity); // 100 * 2 = 200
        result.Data!.Items.Count.Should().Be(1);
        result.Data!.Items.First().Price.Should().Be(price);

        // Assert 2: State in Memory (Inventory -> ConfirmSale, Cart -> Clear)
        product.Inventory.QuantityOnHand.Should().Be(8); // 10 - 2 = 8
        cart.Items.Should().BeEmpty();

        // Assert 3: Repository
        _orderRepo.Verify(x => x.AddASync(It.IsAny<Order>()), Times.Once);
        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task Checkout_Fail_CartIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync((Cart?)null);

        // Act
        var result = await _serivce.CheckoutAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cart is empty");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);

        _orderRepo.Verify(x => x.AddASync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_Fail_CartNoItems()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyCart = new Cart { UserId = userId, Items = new List<CartItem>() };
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync(emptyCart);

        // Act
        var result = await _serivce.CheckoutAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cart is empty");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);

        _orderRepo.Verify(x => x.AddASync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Checkout_Fail_StockIsInsufficient()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // OnHand 5, Reserved 4 -> Available 1
        var product = new Product
        {
            Name = "Test Product",
            Sku = "test-sku",
            Slug = "test-slug",
            Price = Money.Create(100),
            Inventory = new ProductInventory
            {
                QuantityOnHand = 5,
                QuantityReserved = 4 // Available = 1
            }
        };

        //  Quantity > Available
        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { Quantity = 2, Product = product }
            }
        };

        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync(cart);

        // Act
        var result = await _serivce.CheckoutAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("out of stock");
        result.ErrorCode.Should().Be(ErrorCode.Conflict);

        _orderRepo.Verify(x => x.AddASync(It.IsAny<Order>()), Times.Never);
        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }
}