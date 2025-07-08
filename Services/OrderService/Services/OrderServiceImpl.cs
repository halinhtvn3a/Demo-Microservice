using AutoMapper;
using Dapr.Client;
using Dapr.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Workflows;
using Shared.Models;
using Shared.Models.DTOs;
using Shared.Models.Events;

namespace OrderService.Services;

public class OrderServiceImpl : IOrderService
{
	private readonly OrderDbContext _context;
	private readonly IMapper _mapper;
	private readonly HybridCache _cache;
	private readonly DaprClient _daprClient;
	private readonly DaprWorkflowClient _workflowClient;
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
		DaprClient daprClient,
		DaprWorkflowClient workflowClient,
		IUserServiceClient userServiceClient,
		IProductServiceClient productServiceClient,
		ILogger<OrderServiceImpl> logger)
	{
		_context = context;
		_mapper = mapper;
		_cache = cache;
		_daprClient = daprClient;
		_workflowClient = workflowClient;
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
			var cachedOrder = await _cache.GetAsync<OrderDto>(cacheKey);
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
			await _cache.SetAsync(cacheKey, orderDto, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

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
			var cachedOrders = await _cache.GetAsync<IEnumerable<OrderDto>>(cacheKey);
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
			await _cache.SetAsync(cacheKey, orderDtos, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

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
					UnitPrice = item.UnitPrice ?? 0, // Will be filled from product service
					TotalPrice = (item.UnitPrice ?? 0) * item.Quantity
				}).ToList()
			};

			_context.Orders.Add(order);
			await _context.SaveChangesAsync();

			var orderDto = _mapper.Map<OrderDto>(order);

			// Publish order created event
			var orderCreatedEvent = new OrderCreatedEvent
			{
				OrderId = order.Id,
				OrderNumber = order.OrderNumber,
				UserId = order.UserId,
				TotalAmount = order.TotalAmount,
				CreatedAt = order.CreatedAt
			};

			await _daprClient.PublishEventAsync("pubsub", "order-created", orderCreatedEvent);

			// Start workflow
			var workflowId = await StartOrderProcessingWorkflowAsync(order.Id);
			_logger.LogInformation("Order {OrderId} created and workflow {WorkflowId} started", order.Id, workflowId);

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
			var statusUpdatedEvent = new OrderStatusUpdatedEvent
			{
				OrderId = order.Id,
				OrderNumber = order.OrderNumber,
				OldStatus = oldStatus,
				NewStatus = status,
				UpdatedAt = DateTime.UtcNow
			};

			await _daprClient.PublishEventAsync("pubsub", "order-status-updated", statusUpdatedEvent);

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
			var cancelledEvent = new OrderCancelledEvent
			{
				OrderId = order.Id,
				OrderNumber = order.OrderNumber,
				UserId = order.UserId,
				Reason = reason,
				CancelledAt = DateTime.UtcNow
			};

			await _daprClient.PublishEventAsync("pubsub", "order-cancelled", cancelledEvent);

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
		try
		{
			var order = await _context.Orders
				.Include(o => o.Items)
				.FirstOrDefaultAsync(o => o.Id == orderId);

			if (order == null)
			{
				throw new ArgumentException($"Order {orderId} not found");
			}

			var workflowInput = new OrderProcessingInput
			{
				OrderId = order.Id,
				UserId = order.UserId,
				TotalAmount = order.TotalAmount,
				Items = order.Items.Select(item => new OrderItemInput
				{
					ProductId = item.ProductId,
					Quantity = item.Quantity,
					UnitPrice = item.UnitPrice
				}).ToList()
			};

			var workflowId = $"order-processing-{orderId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

			await _workflowClient.ScheduleNewWorkflowAsync(
				nameof(OrderProcessingWorkflow),
				workflowId,
				workflowInput);

			_logger.LogInformation("Started workflow {WorkflowId} for order {OrderId}", workflowId, orderId);
			return workflowId;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error starting workflow for order {OrderId}", orderId);
			throw;
		}
	}

	public async Task<bool> ApproveOrderAsync(int orderId)
	{
		try
		{
			// This would typically raise an external event to the workflow
			var workflowId = $"order-processing-{orderId}";

			await _workflowClient.RaiseEventAsync(workflowId, "OrderApproval", true);

			_logger.LogInformation("Order {OrderId} approved", orderId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error approving order {OrderId}", orderId);
			return false;
		}
	}

	public async Task<bool> RejectOrderAsync(int orderId, string reason)
	{
		try
		{
			var workflowId = $"order-processing-{orderId}";

			await _workflowClient.RaiseEventAsync(workflowId, "OrderApproval", false);

			_logger.LogInformation("Order {OrderId} rejected. Reason: {Reason}", orderId, reason);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error rejecting order {OrderId}", orderId);
			return false;
		}
	}

	public async Task<object> GetWorkflowStatusAsync(string workflowId)
	{
		try
		{
			var status = await _workflowClient.GetWorkflowStateAsync(workflowId);
			return new
			{
				workflowId,
				runtimeStatus = status?.RuntimeStatus?.ToString(),
				createdAt = status?.CreatedAt,
				lastUpdatedAt = status?.LastUpdatedAt,
				output = status?.ReadOutputAs<OrderProcessingResult>()
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting workflow status for {WorkflowId}", workflowId);
			return new { error = ex.Message };
		}
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
					total += product.Price * item.Quantity;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Could not get price for product {ProductId}", item.ProductId);
				// Use provided unit price if available
				if (item.UnitPrice.HasValue)
				{
					total += item.UnitPrice.Value * item.Quantity;
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