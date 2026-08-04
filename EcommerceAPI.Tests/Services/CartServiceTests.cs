using EcommerceAPI.Application.DTOs.Carts;
using EcommerceAPI.Application.Interfaces.Repositories;
using EcommerceAPI.Application.Services;
using EcommerceAPI.Domain.Entities;
using EcommerceAPI.Domain.Shared;
using FluentAssertions;
using Moq;

namespace EcommerceAPI.Tests.Services;

public class CartServiceTests
{
    private readonly Mock<ICartRepository> _cartRepo = new Mock<ICartRepository>();
    private readonly Mock<IProductRepository> _productRepo = new Mock<IProductRepository>();
    private readonly CartService _service;

    public CartServiceTests()
    {
        _service = new CartService(_cartRepo.Object, _productRepo.Object);
    }

    [Fact]
    public async Task AddItem_Success()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync((Cart?)null);

        var product = new Product
        {
            Name = "Test Product",
            Slug = "test-product",
            Sku = "test-001",
            Inventory = new ProductInventory
            {
                QuantityOnHand = 10,
                QuantityReserved = 0
            }
        };
        var productId = product.Id;
        _productRepo.Setup(x => x.GetByIdAsync(productId, true)).ReturnsAsync(product);

        // Act
        var result = await _service.AddItemAsync(
            userId,
            new AddToCartRequest { ProductId = productId, Quantity = 2 }
        );

        // Asserts
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        // Verify
        _cartRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task AddItem_Fail_StockInsufficient()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync((Cart?)null);

        var product = new Product
        {
            Name = "Test Product",
            Slug = "test-product",
            Sku = "test-001",
            Inventory = new ProductInventory
            {
                QuantityOnHand = 10,
                QuantityReserved = 0
            }
        };
        var productId = product.Id;
        _productRepo.Setup(x => x.GetByIdAsync(productId, true)).ReturnsAsync(product);

        // Act
        var result = await _service.AddItemAsync(
            userId,
            new AddToCartRequest { ProductId = productId, Quantity = 20 }
        );

        // Asserts
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.Conflict);

        _cartRepo.Verify(x => x.SaveChangeAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateItemQuantity_Success_ReserveStock()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Slug = "test-product",
            Sku = "test-001",
            Inventory = new ProductInventory
            {
                QuantityOnHand = 10,
                QuantityReserved = 1
            }
        };
        var productId = product.Id;
        _productRepo.Setup(x => x.GetByIdAsync(productId, true)).ReturnsAsync(product);

        var cart = new Cart
        {
            Items = new List<CartItem>
            {
                new() { ProductId = productId, Quantity = 1 }
            }
        };
        var userId = Guid.NewGuid();
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync(cart);

        // Act
        var result = await _service.UpdateItemQuantityAsync(userId, new UpdateCartRequest
        {
            ProductId = productId,
            NewQuantity = 5
        });

        // Asserts
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        // Asserts 2
        product.Inventory.QuantityReserved.Should().Be(5); // From 1 -> 5
        product.Inventory.AvailableQuantity.Should().Be(5);

        // Asserts 3
        cart.Items.First().Quantity.Should().Be(5);

        _cartRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateItemQuantity_Success_ReleaseStock()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Slug = "test-product",
            Sku = "test-001",
            Inventory = new ProductInventory
            {
                QuantityOnHand = 10,
                QuantityReserved = 10
            }
        };
        var productId = product.Id;
        _productRepo.Setup(x => x.GetByIdAsync(productId, true)).ReturnsAsync(product);

        var cart = new Cart
        {
            Items = new List<CartItem>
            {
                new() { ProductId = productId, Quantity = 10 }
            }
        };
        var userId = Guid.NewGuid();
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId)).ReturnsAsync(cart);

        // Act
        var result = await _service.UpdateItemQuantityAsync(userId, new UpdateCartRequest
        {
            ProductId = productId,
            NewQuantity = 2
        });

        // Asserts
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();

        // Asserts 2
        product.Inventory.QuantityReserved.Should().Be(2); // From 10 -> 2
        product.Inventory.AvailableQuantity.Should().Be(8);

        // Asserts 3
        cart.Items.First().Quantity.Should().Be(2);

        _cartRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveItem_Success()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product",
            Slug = "test-product",
            Sku = "test-001",
            Inventory = new ProductInventory
            {
                QuantityOnHand = 10,
                QuantityReserved = 5
            },
        };
        var productId = product.Id;

        var userId = Guid.NewGuid();
        var request = new RemoveCartItemRequest { ProductId = productId };

        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { ProductId = productId, Quantity = 5 }
            }
        };

        _productRepo.Setup(x => x.GetByIdAsync(productId, It.IsAny<bool>()))
            .ReturnsAsync(product);
        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.RemoveItemAsync(userId, request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Inventory Reserved: 5 (5 - 5 = 0)
        product.Inventory.QuantityReserved.Should().Be(0);

        cart.Items.Should().BeEmpty();

        // Assert (Behavior) Repository
        _cartRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }

    [Fact]
    public async Task ClearCart_Success()
    {
        var userId = Guid.NewGuid();

        var product1 = new Product
        {
            Name = "Test Product 1",
            Slug = "test-product1",
            Sku = "test-001",
            Inventory = new ProductInventory { QuantityOnHand = 10, QuantityReserved = 3 }
        };
        var productId1 = product1.Id;

        var product2 = new Product
        {
            Name = "Test Product 2",
            Slug = "test-product2",
            Sku = "test-002",
            Inventory = new ProductInventory { QuantityOnHand = 20, QuantityReserved = 7 }
        };
        var productId2 = product2.Id;

        // Cart
        var cart = new Cart
        {
            UserId = userId,
            Items = new List<CartItem>
            {
                new() { ProductId = productId1, Quantity = 3, Product = product1 },
                new() { ProductId = productId2, Quantity = 7, Product = product2 }
            }
        };

        _cartRepo.Setup(x => x.GetCartByUserIdAsync(userId))
            .ReturnsAsync(cart);

        // Act
        var result = await _service.ClearCartAsync(userId);

        // Assert (State)
        result.IsSuccess.Should().BeTrue();

        product1.Inventory.QuantityReserved.Should().Be(0); // 3 - 3 = 0
        product2.Inventory.QuantityReserved.Should().Be(0); // 7 - 7 = 0

        cart.Items.Should().BeEmpty();

        _cartRepo.Verify(x => x.SaveChangeAsync(), Times.Once);
    }
}