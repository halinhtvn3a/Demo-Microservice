namespace OrderService.Services;

public class MockOrderGrpcService : IOrderGrpcService
{
    private readonly ILogger<MockOrderGrpcService> _logger;

    public MockOrderGrpcService(ILogger<MockOrderGrpcService> logger)
    {
        _logger = logger;
    }

    public Task<bool> ValidateOrderItemsViaGrpcAsync(List<(int ProductId, int Quantity)> items)
    {
        _logger.LogInformation("Mock gRPC validate {ItemCount} order items", items.Count);
        return Task.FromResult(true);
    }

    public Task<bool> ReserveStockViaGrpcAsync(int orderId)
    {
        _logger.LogInformation("Mock gRPC reserve stock for order {OrderId}", orderId);
        return Task.FromResult(true);
    }

    public Task<bool> ReleaseStockViaGrpcAsync(int orderId)
    {
        _logger.LogInformation("Mock gRPC release stock for order {OrderId}", orderId);
        return Task.FromResult(true);
    }
}