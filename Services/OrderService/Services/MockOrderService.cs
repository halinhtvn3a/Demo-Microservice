using Shared.Models;
using Shared.Models.DTOs;

namespace OrderService.Services;

public class MockOrderService : IOrderService
{
    private readonly ILogger<MockOrderService> _logger;
    private static int _orderIdCounter = 1;

    public MockOrderService(ILogger<MockOrderService> logger)
    {
        _logger = logger;
    }

    public Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        _logger.LogInformation("Mock get order {OrderId}", id);
        return Task.FromResult<OrderDto?>(new OrderDto
        {
            Id = id,
            OrderNumber = $"ORD{DateTime.UtcNow:yyyyMMdd}{id:D4}",
            UserId = 1,
            ShippingAddress = "Mock Address",
            TotalAmount = 35.50m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = new List<OrderItemDto>
            {
                new() { Id = 1, ProductId = 1, Quantity = 2, UnitPrice = 10.50m },
                new() { Id = 2, ProductId = 2, Quantity = 1, UnitPrice = 25.00m }
            }
        });
    }

    public Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        _logger.LogInformation("Mock get order by number {OrderNumber}", orderNumber);
        return GetOrderByIdAsync(1);
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("Mock get orders for user {UserId}", userId);
        var order = await GetOrderByIdAsync(1);
        var orders = new List<OrderDto> { order! };
        return orders;
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(OrderSearchRequest request)
    {
        _logger.LogInformation("Mock search orders");
        var order = await GetOrderByIdAsync(1);
        var orders = new List<OrderDto> { order! };
        return orders;
    }

    public Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation("Mock create order for user {UserId} with {ItemCount} items",
            request.UserId, request.Items.Count);

        var orderId = _orderIdCounter++;
        var totalAmount = request.Items.Sum(i => i.UnitPrice * i.Quantity);

        var orderDto = new OrderDto
        {
            Id = orderId,
            OrderNumber = $"ORD{DateTime.UtcNow:yyyyMMdd}{orderId:D4}",
            UserId = request.UserId,
            ShippingAddress = request.ShippingAddress,
            TotalAmount = totalAmount,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select((item, index) => new OrderItemDto
            {
                Id = index + 1,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        _logger.LogInformation("Mock order {OrderId} created successfully with total {TotalAmount}",
            orderId, totalAmount);

        return Task.FromResult<OrderDto?>(orderDto);
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(int id, OrderStatus status)
    {
        _logger.LogInformation("Mock update order {OrderId} status to {Status}", id, status);
        var order = (await GetOrderByIdAsync(id))!;
        order.Status = status;
        return order;
    }

    public Task<bool> CancelOrderAsync(int id, string reason)
    {
        _logger.LogInformation("Mock cancel order {OrderId} with reason: {Reason}", id, reason);
        return Task.FromResult(true);
    }

    public Task<string> StartOrderProcessingWorkflowAsync(int orderId)
    {
        _logger.LogInformation("Mock start workflow for order {OrderId}", orderId);
        return Task.FromResult($"mock-workflow-{orderId}");
    }

    public Task<bool> ApproveOrderAsync(int orderId)
    {
        _logger.LogInformation("Mock approve order {OrderId}", orderId);
        return Task.FromResult(true);
    }

    public Task<bool> RejectOrderAsync(int orderId, string reason)
    {
        _logger.LogInformation("Mock reject order {OrderId} with reason: {Reason}", orderId, reason);
        return Task.FromResult(true);
    }

    public Task<object> GetWorkflowStatusAsync(string workflowId)
    {
        _logger.LogInformation("Mock get workflow status {WorkflowId}", workflowId);
        return Task.FromResult<object>(new { workflowId, status = "completed", message = "Mock workflow" });
    }

    public Task<decimal> CalculateOrderTotalAsync(List<CreateOrderItemRequest> items)
    {
        _logger.LogInformation("Mock calculate order total for {ItemCount} items", items.Count);
        var total = items.Sum(i => i.UnitPrice * i.Quantity);
        return Task.FromResult(total);
    }

    public Task<bool> ValidateOrderItemsAsync(List<CreateOrderItemRequest> items)
    {
        _logger.LogInformation("Mock validate {ItemCount} order items", items.Count);
        return Task.FromResult(true);
    }

    public Task<OrderDto?> GetOrderWithItemsAsync(int id)
    {
        _logger.LogInformation("Mock get order with items {OrderId}", id);
        return GetOrderByIdAsync(id);
    }

    public Task InvalidateOrderCacheAsync(int orderId)
    {
        _logger.LogInformation("Mock invalidate order cache {OrderId}", orderId);
        return Task.CompletedTask;
    }

    public Task InvalidateUserOrdersCacheAsync(int userId)
    {
        _logger.LogInformation("Mock invalidate user orders cache {UserId}", userId);
        return Task.CompletedTask;
    }
}