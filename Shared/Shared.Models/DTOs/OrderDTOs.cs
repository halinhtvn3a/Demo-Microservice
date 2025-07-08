using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs;

public class OrderDto
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public OrderStatus Status { get; set; }
	public decimal TotalAmount { get; set; }
	public string ShippingAddress { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
	public int Id { get; set; }
	public int ProductId { get; set; }
	public string ProductName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
	public decimal TotalPrice { get; set; }
}

public class CreateOrderRequest
{
	[Required]
	public int UserId { get; set; }

	[Required]
	public string ShippingAddress { get; set; } = string.Empty;

	[Required]
	[MinLength(1)]
	public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
	[Required]
	public int ProductId { get; set; }

	[Required]
	[Range(1, int.MaxValue)]
	public int Quantity { get; set; }
}

public class UpdateOrderStatusRequest
{
	[Required]
	public OrderStatus Status { get; set; }
}

public class OrderSearchRequest
{
	public int? UserId { get; set; }
	public OrderStatus? Status { get; set; }
	public DateTime? FromDate { get; set; }
	public DateTime? ToDate { get; set; }
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 10;
}