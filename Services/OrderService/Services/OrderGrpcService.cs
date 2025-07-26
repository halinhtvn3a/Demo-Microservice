using OrderService.Clients;
using OrderService.Data;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Services;

/// <summary>
/// Service demonstrating gRPC usage for high-performance operations
/// </summary>
public interface IOrderGrpcService
{
    Task<bool> ValidateOrderItemsViaGrpcAsync(List<(int ProductId, int Quantity)> items);
    Task<bool> ReserveStockViaGrpcAsync(int orderId);
    Task<bool> ReleaseStockViaGrpcAsync(int orderId);
}

public class OrderGrpcService : IOrderGrpcService
{
    private readonly IProductGrpcClient _productGrpcClient;
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderGrpcService> _logger;

    public OrderGrpcService(
        IProductGrpcClient productGrpcClient,
        OrderDbContext context,
        ILogger<OrderGrpcService> logger)
    {
        _productGrpcClient = productGrpcClient;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ValidateOrderItemsViaGrpcAsync(List<(int ProductId, int Quantity)> items)
    {
        _logger.LogInformation("Validating {Count} order items via gRPC", items.Count);

        try
        {
            foreach (var (productId, quantity) in items)
            {
                // Use gRPC for high-performance stock checking
                var stockResponse = await _productGrpcClient.CheckStockAsync(productId, quantity);

                if (!stockResponse.Available)
                {
                    _logger.LogWarning("Insufficient stock for product {ProductId}. Required: {Required}, Available: {Available}",
                        productId, quantity, stockResponse.CurrentStock);
                    return false;
                }

                // Also validate product exists and is active
                var productResponse = await _productGrpcClient.GetProductAsync(productId);
                if (!productResponse.IsActive)
                {
                    _logger.LogWarning("Product {ProductId} is not active", productId);
                    return false;
                }
            }

            _logger.LogInformation("All order items validated successfully via gRPC");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order items via gRPC");
            return false;
        }
    }

    public async Task<bool> ReserveStockViaGrpcAsync(int orderId)
    {
        _logger.LogInformation("Reserving stock for order {OrderId} via gRPC", orderId);

        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", orderId);
                return false;
            }

            // Reserve stock for each item using gRPC
            foreach (var item in order.Items)
            {
                var response = await _productGrpcClient.UpdateStockAsync(item.ProductId, -item.Quantity);

                if (!response.Success)
                {
                    _logger.LogError("Failed to reserve stock for product {ProductId} in order {OrderId}",
                        item.ProductId, orderId);

                    // TODO: Implement compensation logic to rollback previous reservations
                    return false;
                }

                _logger.LogInformation("Reserved {Quantity} units of product {ProductId} for order {OrderId}",
                    item.Quantity, item.ProductId, orderId);
            }

            _logger.LogInformation("Successfully reserved stock for order {OrderId} via gRPC", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for order {OrderId} via gRPC", orderId);
            return false;
        }
    }

    public async Task<bool> ReleaseStockViaGrpcAsync(int orderId)
    {
        _logger.LogInformation("Releasing stock for order {OrderId} via gRPC", orderId);

        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", orderId);
                return false;
            }

            // Release stock for each item using gRPC
            foreach (var item in order.Items)
            {
                var response = await _productGrpcClient.UpdateStockAsync(item.ProductId, item.Quantity);

                if (!response.Success)
                {
                    _logger.LogError("Failed to release stock for product {ProductId} in order {OrderId}",
                        item.ProductId, orderId);
                    return false;
                }

                _logger.LogInformation("Released {Quantity} units of product {ProductId} for order {OrderId}",
                    item.Quantity, item.ProductId, orderId);
            }

            _logger.LogInformation("Successfully released stock for order {OrderId} via gRPC", orderId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing stock for order {OrderId} via gRPC", orderId);
            return false;
        }
    }
}