using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Services;
using Shared.Models;
using Shared.Models.DTOs;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
	private readonly IOrderService _orderService;
	private readonly ILogger<OrdersController> _logger;

	public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
	{
		_orderService = orderService;
		_logger = logger;
	}

	/// <summary>
	/// Get all orders with optional filtering
	/// </summary>
	[HttpGet]
	[ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
	public async Task<IActionResult> GetOrders([FromQuery] OrderSearchRequest request)
	{
		var orders = await _orderService.GetOrdersAsync(request);
		return Ok(orders);
	}

	/// <summary>
	/// Get order by ID
	/// </summary>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(OrderDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> GetOrder(int id)
	{
		var order = await _orderService.GetOrderByIdAsync(id);
		if (order == null)
		{
			return NotFound();
		}

		return Ok(order);
	}

	/// <summary>
	/// Get order by order number
	/// </summary>
	[HttpGet("by-number/{orderNumber}")]
	[ProducesResponseType(typeof(OrderDto), 200)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> GetOrderByNumber(string orderNumber)
	{
		var order = await _orderService.GetOrderByNumberAsync(orderNumber);
		if (order == null)
		{
			return NotFound();
		}

		return Ok(order);
	}

	/// <summary>
	/// Get orders for a specific user
	/// </summary>
	[HttpGet("user/{userId}")]
	[ProducesResponseType(typeof(IEnumerable<OrderDto>), 200)]
	public async Task<IActionResult> GetUserOrders(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
	{
		var orders = await _orderService.GetOrdersByUserAsync(userId, page, pageSize);
		return Ok(orders);
	}

	/// <summary>
	/// Create a new order
	/// </summary>
	[HttpPost]
	[ProducesResponseType(typeof(OrderDto), 201)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var order = await _orderService.CreateOrderAsync(request);
		if (order == null)
		{
			return BadRequest(new { message = "Failed to create order" });
		}

		_logger.LogInformation("Order {OrderId} created successfully", order.Id);
		return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
	}

	/// <summary>
	/// Update order status
	/// </summary>
	[HttpPatch("{id}/status")]
	[ProducesResponseType(typeof(OrderDto), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var order = await _orderService.UpdateOrderStatusAsync(id, request.Status);
		if (order == null)
		{
			return NotFound();
		}

		_logger.LogInformation("Order {OrderId} status updated to {Status}", id, request.Status);
		return Ok(order);
	}

	/// <summary>
	/// Cancel an order
	/// </summary>
	[HttpPost("{id}/cancel")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
	{
		var success = await _orderService.CancelOrderAsync(id, request.Reason);
		if (!success)
		{
			return BadRequest(new { message = "Failed to cancel order" });
		}

		_logger.LogInformation("Order {OrderId} cancelled", id);
		return Ok(new { message = "Order cancelled successfully" });
	}

	/// <summary>
	/// Approve an order (for high-value orders)
	/// </summary>
	[HttpPost("{id}/approve")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> ApproveOrder(int id)
	{
		var success = await _orderService.ApproveOrderAsync(id);
		if (!success)
		{
			return BadRequest(new { message = "Failed to approve order" });
		}

		_logger.LogInformation("Order {OrderId} approved", id);
		return Ok(new { message = "Order approved successfully" });
	}

	/// <summary>
	/// Reject an order (for high-value orders)
	/// </summary>
	[HttpPost("{id}/reject")]
	[ProducesResponseType(200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> RejectOrder(int id, [FromBody] RejectOrderRequest request)
	{
		var success = await _orderService.RejectOrderAsync(id, request.Reason);
		if (!success)
		{
			return BadRequest(new { message = "Failed to reject order" });
		}

		_logger.LogInformation("Order {OrderId} rejected", id);
		return Ok(new { message = "Order rejected successfully" });
	}

	/// <summary>
	/// Get workflow status for an order
	/// </summary>
	[HttpGet("{id}/workflow-status")]
	[ProducesResponseType(typeof(object), 200)]
	public async Task<IActionResult> GetWorkflowStatus(int id)
	{
		var workflowId = $"order-processing-{id}";
		var status = await _orderService.GetWorkflowStatusAsync(workflowId);
		return Ok(status);
	}

	/// <summary>
	/// Health check endpoint
	/// </summary>
	[HttpGet("health")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(object), 200)]
	public IActionResult Health()
	{
		return Ok(new
		{
			status = "healthy",
			service = "OrderService",
			timestamp = DateTime.UtcNow
		});
	}
}

// Request DTOs
public class UpdateOrderStatusRequest
{
	public OrderStatus Status { get; set; }
}

public class CancelOrderRequest
{
	public string Reason { get; set; } = string.Empty;
}

public class RejectOrderRequest
{
	public string Reason { get; set; } = string.Empty;
}