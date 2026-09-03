using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Interfaces.Services;
using EcommerceAPI.Application.Services;
using EcommerceAPI.Domain.Common.ValueObject;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Enums;
using EcommerceAPI.Domain.Shared;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.Tests.Services;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new Mock<IOrderRepository>();
    private readonly Mock<ICartRepository> _cartRepo = new Mock<ICartRepository>();
    private readonly Mock<ICurrentUserService> _currentUser = new Mock<ICurrentUserService>();
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _service = new OrderService(_orderRepo.Object, _cartRepo.Object, _currentUser.Object);
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
                QuantityReserved = quantity
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

        _currentUser.Setup(x => x.UserId).Returns(userId);
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync(cart);
        _orderRepo.Setup(x => x.AddASync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _orderRepo.Setup(x => x.SaveChangeAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CheckoutAsync();

        // Assert 1
        result.IsSuccess.Should().BeTrue();
        result.Data!.TotalPrice.Should().Be(price * quantity);
        result.Data!.Items.Count.Should().Be(1);
        result.Data!.Items.First().Price.Should().Be(price);

        // Assert 2: State in Memory (Inventory -> ConfirmSale, Cart -> Clear)
        product.Inventory.QuantityOnHand.Should().Be(8); // 10 - 2 = 8
        product.Inventory.QuantityReserved.Should().Be(0); // 2 - 2 = 0
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
        _currentUser.Setup(x => x.UserId).Returns(userId);
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync((Cart?)null);

        // Act
        var result = await _service.CheckoutAsync();

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
        _currentUser.Setup(x => x.UserId).Returns(userId);
        var emptyCart = new Cart { UserId = userId, Items = new List<CartItem>() };
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync(emptyCart);

        // Act
        var result = await _service.CheckoutAsync();

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
                QuantityReserved = 0
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

        _currentUser.Setup(x => x.UserId).Returns(userId);
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync(cart);

        // Act
        var result = await _service.CheckoutAsync();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);

        _orderRepo.Verify(x => x.AddASync(It.IsAny<Order>()), Times.Never);
        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_Success_WhenUserIsOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Test Product",
            Sku = "test-sku",
            Slug = "test-slug",
            Price = Money.Create(50m),
            Inventory = new ProductInventory
            {
                QuantityOnHand = 8,
                QuantityReserved = 0
            }
        };

        var order = new Order
        {
            UserId = userId,
            TotalPrice = 100m,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = 2,
                    PriceAtPurchase = 50m,
                    ProductNameAtPurchase = product.Name
                }
            }
        };

        _currentUser.Setup(x => x.UserId).Returns(userId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);
        _orderRepo.Setup(x => x.SaveChangeAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
        product.Inventory.QuantityOnHand.Should().Be(10); // 8 + 2

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelOrder_Success_WhenUserIsAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Test Product",
            Sku = "test-sku",
            Slug = "test-slug",
            Price = Money.Create(50m),
            Inventory = new ProductInventory
            {
                QuantityOnHand = 5,
                QuantityReserved = 0
            }
        };

        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 150m,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = 3,
                    PriceAtPurchase = 50m,
                    ProductNameAtPurchase = product.Name
                }
            }
        };

        _currentUser.Setup(x => x.UserId).Returns(adminId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.Admin);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);
        _orderRepo.Setup(x => x.SaveChangeAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CancelOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
        product.Inventory.QuantityOnHand.Should().Be(8); // 5 + 3

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelOrder_Fail_WhenOrderNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        var result = await _service.CancelOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found");
        result.ErrorCode.Should().Be(ErrorCode.NotFound);

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_Fail_WhenUserIsNotOwnerAndNotAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        };

        _currentUser.Setup(x => x.UserId).Returns(anotherUserId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.CancelOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot cancel order: no permissions");
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_Fail_WhenOrderStatusCannotBeCancelled()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = userId,
            TotalPrice = 100m
        };
        order.Pay();
        order.Ship(); // Status is Shipped, which cannot be cancelled

        _currentUser.Setup(x => x.UserId).Returns(userId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.CancelOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);
        result.ErrorMessage.Should().Be("Status Cancel for Pending and Paid");

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_Fail_WhenReturnStockFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var product = new Product
        {
            Name = "Test Product",
            Sku = "test-sku",
            Slug = "test-slug",
            Price = Money.Create(50m),
            Inventory = new ProductInventory
            {
                QuantityOnHand = 5,
                QuantityReserved = 0
            }
        };

        var order = new Order
        {
            UserId = userId,
            TotalPrice = 0m,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = 0, // Invalid quantity for ReturnStock (<= 0)
                    PriceAtPurchase = 50m,
                    ProductNameAtPurchase = product.Name
                }
            }
        };

        _currentUser.Setup(x => x.UserId).Returns(userId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.CancelOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);
        result.ErrorMessage.Should().Be("Quantity must be greater than zero");

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task PayOrder_Success_WhenUserIsOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = userId,
            TotalPrice = 100m
        };

        _currentUser.Setup(x => x.UserId).Returns(userId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);
        _orderRepo.Setup(x => x.SaveChangeAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.PayOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task PayOrder_Success_WhenUserIsAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        };

        _currentUser.Setup(x => x.UserId).Returns(adminId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.Admin);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);
        _orderRepo.Setup(x => x.SaveChangeAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.PayOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task PayOrder_Fail_WhenOrderNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        var result = await _service.PayOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found");
        result.ErrorCode.Should().Be(ErrorCode.NotFound);

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task PayOrder_Fail_WhenUserIsNotOwnerAndNotAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        };

        _currentUser.Setup(x => x.UserId).Returns(anotherUserId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.PayOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot pay order: no permissions");
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task PayOrder_Fail_WhenOrderStatusCannotBePaid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = userId,
            TotalPrice = 100m
        };
        order.Pay(); // Status is already Paid

        _currentUser.Setup(x => x.UserId).Returns(userId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.PayOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);
        result.ErrorMessage.Should().Be("Status Paid for Pending only");

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task ShipOrder_Success_WhenUserIsAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        };
        order.Pay(); // Status must be Paid to Ship

        _currentUser.Setup(x => x.Role).Returns(UserRoles.Admin);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);
        _orderRepo.Setup(x => x.SaveChangeAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.ShipOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().NotBeNull();

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task ShipOrder_Fail_WhenOrderNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        var result = await _service.ShipOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found");
        result.ErrorCode.Should().Be(ErrorCode.NotFound);

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task ShipOrder_Fail_WhenUserIsNotAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        };
        order.Pay();

        _currentUser.Setup(x => x.UserId).Returns(ownerId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.ShipOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot ship order: admin only");
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task ShipOrder_Fail_WhenOrderStatusCannotBeShipped()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        }; // Status is Pending, not Paid

        _currentUser.Setup(x => x.Role).Returns(UserRoles.Admin);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.ShipOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);
        result.ErrorMessage.Should().Be("Status Shipped for Paid only");

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task CompleteOrder_Success_WhenUserIsAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        };
        order.Pay();
        order.Ship(); // Status must be Shipped to Complete

        _currentUser.Setup(x => x.Role).Returns(UserRoles.Admin);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);
        _orderRepo.Setup(x => x.SaveChangeAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _service.CompleteOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task CompleteOrder_Fail_WhenOrderNotFound()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync((Order?)null);

        // Act
        var result = await _service.CompleteOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Order not found");
        result.ErrorCode.Should().Be(ErrorCode.NotFound);

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task CompleteOrder_Fail_WhenUserIsNotAdmin()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        };
        order.Pay();
        order.Ship();

        _currentUser.Setup(x => x.UserId).Returns(ownerId);
        _currentUser.Setup(x => x.Role).Returns(UserRoles.User);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.CompleteOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Cannot complete order: admin only");
        result.ErrorCode.Should().Be(ErrorCode.Forbidden);

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task CompleteOrder_Fail_WhenOrderStatusCannotBeCompleted()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            UserId = ownerId,
            TotalPrice = 100m
        };
        order.Pay(); // Status is Paid, not Shipped

        _currentUser.Setup(x => x.Role).Returns(UserRoles.Admin);
        _orderRepo.Setup(x => x.GetOrderByIdAsync(orderId)).ReturnsAsync(order);

        // Act
        var result = await _service.CompleteOrderAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);
        result.ErrorMessage.Should().Be("Status Completed for Shipped only");

        _orderRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }
}