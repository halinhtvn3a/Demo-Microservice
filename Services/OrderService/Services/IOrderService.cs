using Shared.Models;
using Shared.Models.DTOs;

namespace OrderService.Services;

public interface IOrderService
{
	// Order CRUD operations
	Task<OrderDto?> GetOrderByIdAsync(int id);
	Task<OrderDto?> GetOrderByNumberAsync(string orderNumber);
	Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(int userId, int page = 1, int pageSize = 10);
	Task<IEnumerable<OrderDto>> GetOrdersAsync(OrderSearchRequest request);
	Task<OrderDto?> CreateOrderAsync(CreateOrderRequest request);
	Task<OrderDto?> UpdateOrderStatusAsync(int id, OrderStatus status);
	Task<bool> CancelOrderAsync(int id, string reason);

	// Order workflow operations
	Task<string> StartOrderProcessingWorkflowAsync(int orderId);
	Task<bool> ApproveOrderAsync(int orderId);
	Task<bool> RejectOrderAsync(int orderId, string reason);
	Task<object> GetWorkflowStatusAsync(string workflowId);

	// Business operations
	Task<decimal> CalculateOrderTotalAsync(List<CreateOrderItemRequest> items);
	Task<bool> ValidateOrderItemsAsync(List<CreateOrderItemRequest> items);
	Task<OrderDto?> GetOrderWithItemsAsync(int id);

	// Cache operations
	Task InvalidateOrderCacheAsync(int orderId);
	Task InvalidateUserOrdersCacheAsync(int userId);
}