using AutoMapper;
// using Dapr.Client; // Removed Dapr dependency
// using Dapr.Workflow; // Removed Dapr dependency
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderService.Clients;
using OrderService.Data;
// using OrderService.Workflows; // Removed Dapr workflow dependency
using Shared.Models;
using Shared.Models.DTOs;
using Shared.Messaging;
using Shared.Events;

namespace OrderService.Services;

public class OrderServiceImpl : IOrderService
{
    private readonly OrderDbContext _context;
    private readonly IMapper _mapper;
    private readonly HybridCache _cache;
    private readonly IMessagePublisher _messagePublisher;
    private readonly IUserServiceClient _userServiceClient;
    private readonly IProductServiceClient _productServiceClient;
    private readonly ILogger<OrderServiceImpl> _logger;

    private const string ORDER_CACHE_KEY = "order:";
    private const string USER_ORDERS_CACHE_KEY = "user_orders:";
    private const int CACHE_DURATION_MINUTES = 10;

    public OrderServiceImpl(
        OrderDbContext context,
        IMapper mapper,
        HybridCache cache,
        IMessagePublisher messagePublisher,
        IUserServiceClient userServiceClient,
        IProductServiceClient productServiceClient,
        ILogger<OrderServiceImpl> logger)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
        _messagePublisher = messagePublisher;
        _userServiceClient = userServiceClient;
        _productServiceClient = productServiceClient;
        _logger = logger;
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        try
        {
            var cacheKey = $"{ORDER_CACHE_KEY}{id}";

            // Try cache first
            var cachedOrder = await _cache.GetOrCreateAsync<OrderDto>(cacheKey, _ => ValueTask.FromResult<OrderDto?>(null), new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableUnderlyingData | HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite });
            if (cachedOrder != null)
            {
                _logger.LogInformation("Order {OrderId} retrieved from cache", id);
                return cachedOrder;
            }

            // Get from database with items
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found", id);
                return null;
            }

            var orderDto = _mapper.Map<OrderDto>(order);

            // Cache for future requests
            await _cache.SetAsync(cacheKey, orderDto, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES) });

            _logger.LogInformation("Order {OrderId} retrieved from database and cached", id);
            return orderDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order by ID: {OrderId}", id);
            return null;
        }
    }

    public async Task<OrderDto?> GetOrderByNumberAsync(string orderNumber)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

            if (order == null)
            {
                _logger.LogWarning("Order with number {OrderNumber} not found", orderNumber);
                return null;
            }

            return _mapper.Map<OrderDto>(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order by number: {OrderNumber}", orderNumber);
            return null;
        }
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId, int page = 1, int pageSize = 10)
    {
        try
        {
            var cacheKey = $"{USER_ORDERS_CACHE_KEY}{userId}:{page}:{pageSize}";

            // Try cache first
            var cachedOrders = await _cache.GetOrCreateAsync<IEnumerable<OrderDto>>(cacheKey, _ => ValueTask.FromResult<IEnumerable<OrderDto>?>(null), new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableUnderlyingData | HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite });
            if (cachedOrders != null)
            {
                _logger.LogInformation("Orders for user {UserId} retrieved from cache", userId);
                return cachedOrders;
            }

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);

            // Cache results
            await _cache.SetAsync(cacheKey, orderDtos, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES) });

            _logger.LogInformation("Retrieved {Count} orders for user {UserId}", orders.Count, userId);
            return orderDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
            return Enumerable.Empty<OrderDto>();
        }
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(OrderSearchRequest request)
    {
        try
        {
            var query = _context.Orders.Include(o => o.Items).AsQueryable();

            if (request.UserId.HasValue)
            {
                query = query.Where(o => o.UserId == request.UserId.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(o => o.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= request.ToDate.Value);
            }

            if (request.MinAmount.HasValue)
            {
                query = query.Where(o => o.TotalAmount >= request.MinAmount.Value);
            }

            if (request.MaxAmount.HasValue)
            {
                query = query.Where(o => o.TotalAmount <= request.MaxAmount.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching orders");
            return Enumerable.Empty<OrderDto>();
        }
    }

    public async Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request)
    {
        try
        {
            // Validate order items
            if (!await ValidateOrderItemsAsync(request.Items))
            {
                _logger.LogWarning("Order validation failed for user {UserId}", request.UserId);
                return null;
            }

            // Calculate total
            var totalAmount = await CalculateOrderTotalAsync(request.Items);

            // Generate order number
            var orderNumber = await GenerateOrderNumberAsync();

            // Create order entity
            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = request.UserId,
                ShippingAddress = request.ShippingAddress,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = request.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice // Use as non-nullable decimal
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderDto = _mapper.Map<OrderDto>(order);

            // Publish order created event
            var orderCreatedEvent = new OrderCreatedEvent(
                order.Id,
                order.UserId,
                order.TotalAmount,
                order.Items.Select(i => new OrderItemEvent(i.ProductId, i.Quantity, i.UnitPrice)).ToList(),
                order.CreatedAt
            );

            // Publish OrderCreatedEvent via RabbitMQ
            await _messagePublisher.PublishAsync("order.created", orderCreatedEvent);

            // TODO: Replace with Hangfire background job
            // var workflowId = await StartOrderProcessingWorkflowAsync(order.Id);
            _logger.LogInformation("Order {OrderId} created", order.Id);

            // Invalidate cache
            await InvalidateUserOrdersCacheAsync(request.UserId);

            return orderDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user {UserId}", request.UserId);
            return null;
        }
    }

    public async Task<OrderDto?> UpdateOrderStatusAsync(int id, OrderStatus status)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for status update", id);
                return null;
            }

            var oldStatus = order.Status;
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publish status updated event
            var statusUpdatedEvent = new OrderStatusChangedEvent(
                order.Id,
                oldStatus.ToString(),
                status.ToString(),
                DateTime.UtcNow
            );

            // Publish OrderStatusChangedEvent via RabbitMQ
            await _messagePublisher.PublishAsync("order.status-changed", statusUpdatedEvent);

            // Invalidate cache
            await InvalidateOrderCacheAsync(id);
            await InvalidateUserOrdersCacheAsync(order.UserId);

            _logger.LogInformation("Order {OrderId} status updated from {OldStatus} to {NewStatus}",
                id, oldStatus, status);

            return _mapper.Map<OrderDto>(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for order {OrderId}", id);
            return null;
        }
    }

    public async Task<bool> CancelOrderAsync(int id, string reason)
    {
        try
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for cancellation", id);
                return false;
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            {
                _logger.LogWarning("Order {OrderId} cannot be cancelled in status {Status}", id, order.Status);
                return false;
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Publish cancellation event
            var cancelledEvent = new OrderCancelledEvent(
                order.Id,
                order.UserId,
                reason,
                DateTime.UtcNow
            );

            // Publish OrderCancelledEvent via RabbitMQ
            await _messagePublisher.PublishAsync("order.cancelled", cancelledEvent);

            // Invalidate cache
            await InvalidateOrderCacheAsync(id);
            await InvalidateUserOrdersCacheAsync(order.UserId);

            _logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", id, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return false;
        }
    }

    public async Task<string> StartOrderProcessingWorkflowAsync(int orderId)
    {
        // TODO: Replace with Hangfire background job
        _logger.LogInformation("Order processing workflow disabled - using simplified flow for order {OrderId}", orderId);
        await Task.CompletedTask;
        return $"simplified-{orderId}";
    }

    public async Task<bool> ApproveOrderAsync(int orderId)
    {
        // TODO: Replace with Hangfire background job
        _logger.LogInformation("Order approval workflow disabled - order {OrderId}", orderId);
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> RejectOrderAsync(int orderId, string reason)
    {
        // TODO: Replace with Hangfire background job
        _logger.LogInformation("Order rejection workflow disabled - order {OrderId}, reason: {Reason}", orderId, reason);
        await Task.CompletedTask;
        return true;
    }

    public async Task<object> GetWorkflowStatusAsync(string workflowId)
    {
        // TODO: Replace with Hangfire job status
        _logger.LogInformation("Workflow status check disabled - workflowId: {WorkflowId}", workflowId);
        await Task.CompletedTask;
        return new { workflowId = workflowId, status = "simplified", message = "Workflow disabled" };
    }

    public async Task<decimal> CalculateOrderTotalAsync(List<CreateOrderItemRequest> items)
    {
        decimal total = 0;

        foreach (var item in items)
        {
            try
            {
                // Get product price from Product Service
                var product = await _productServiceClient.GetProductAsync(item.ProductId);
                if (product != null)
                {
                    if (item.UnitPrice > 0)
                    {
                        total += item.UnitPrice * item.Quantity;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get price for product {ProductId}", item.ProductId);
                // Use provided unit price if available
                if (item.UnitPrice > 0)
                {
                    total += item.UnitPrice * item.Quantity;
                }
            }
        }

        return total;
    }

    public async Task<bool> ValidateOrderItemsAsync(List<CreateOrderItemRequest> items)
    {
        try
        {
            foreach (var item in items)
            {
                // Check if product exists and has sufficient stock
                var stockCheck = await _productServiceClient.CheckStockAsync(item.ProductId, item.Quantity);

                // Note: In a real implementation, you'd parse the response properly
                // For now, we'll assume it's valid if no exception is thrown
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Order item validation failed");
            return false;
        }
    }

    public async Task<OrderDto?> GetOrderWithItemsAsync(int id)
    {
        return await GetOrderByIdAsync(id); // Already includes items
    }

    public async Task InvalidateOrderCacheAsync(int orderId)
    {
        try
        {
            var cacheKey = $"{ORDER_CACHE_KEY}{orderId}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Cache invalidated for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for order {OrderId}", orderId);
        }
    }

    public async Task InvalidateUserOrdersCacheAsync(int userId)
    {
        try
        {
            // In a real implementation, you might want to use cache tags or patterns
            // For now, we'll just log this action
            _logger.LogDebug("User orders cache invalidation requested for user {UserId}", userId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user orders cache for user {UserId}", userId);
        }
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var prefix = "ORD";
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");

        // Get count of orders created today
        var today = DateTime.UtcNow.Date;
        var todayCount = await _context.Orders
            .CountAsync(o => o.CreatedAt.Date == today);

        var sequence = (todayCount + 1).ToString("D4");

        return $"{prefix}{datePart}{sequence}";
    }
}