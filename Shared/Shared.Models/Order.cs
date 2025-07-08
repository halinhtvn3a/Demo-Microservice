using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public class Order
{
	public int Id { get; set; }

	[Required]
	public int UserId { get; set; }

	public User? User { get; set; }

	[Required]
	[StringLength(50)]
	public string OrderNumber { get; set; } = string.Empty;

	[Required]
	public OrderStatus Status { get; set; } = OrderStatus.Pending;

	[Required]
	[Range(0.01, double.MaxValue)]
	public decimal TotalAmount { get; set; }

	public string ShippingAddress { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
	public int Id { get; set; }

	[Required]
	public int OrderId { get; set; }

	public Order? Order { get; set; }

	[Required]
	public int ProductId { get; set; }

	public Product? Product { get; set; }

	[Required]
	[Range(1, int.MaxValue)]
	public int Quantity { get; set; }

	[Required]
	[Range(0.01, double.MaxValue)]
	public decimal UnitPrice { get; set; }

	public decimal TotalPrice => Quantity * UnitPrice;
}

public enum OrderStatus
{
	Pending = 0,
	Confirmed = 1,
	Processing = 2,
	Shipped = 3,
	Delivered = 4,
	Cancelled = 5
}