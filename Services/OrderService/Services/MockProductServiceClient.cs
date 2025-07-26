using OrderService.Clients;
using Shared.Models.DTOs;

namespace OrderService.Services;

public class MockProductServiceClient : IProductServiceClient
{
    private readonly ILogger<MockProductServiceClient> _logger;

    public MockProductServiceClient(ILogger<MockProductServiceClient> logger)
    {
        _logger = logger;
    }

    public Task<ProductDto> GetProductAsync(int id)
    {
        _logger.LogInformation("Mock get product {ProductId}", id);
        return Task.FromResult(new ProductDto
        {
            Id = id,
            Name = $"Mock Product {id}",
            Price = 10.00m,
            Stock = 100
        });
    }

    public Task<object> CheckStockAsync(int id, int quantity)
    {
        _logger.LogInformation("Mock stock check for product {ProductId}, quantity {Quantity}", id, quantity);
        return Task.FromResult<object>(new { available = true, stock = 100 });
    }

    public Task<object> UpdateStockAsync(int id, int quantityChange, string token)
    {
        _logger.LogInformation("Mock stock update for product {ProductId}, change {QuantityChange}", id, quantityChange);
        return Task.FromResult<object>(new { success = true });
    }

    public Task<object> GetHealthAsync()
    {
        return Task.FromResult<object>(new { status = "healthy", service = "MockProductService" });
    }
}