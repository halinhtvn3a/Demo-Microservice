namespace Shared.Models.Events;

public class OrderCreatedEvent
{
	public int OrderId { get; set; }
	public int UserId { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public decimal TotalAmount { get; set; }
	public string UserEmail { get; set; } = string.Empty;
	public string UserFullName { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public List<OrderItemEvent> Items { get; set; } = new();
}

public class OrderItemEvent
{
	public int ProductId { get; set; }
	public string ProductName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
	public decimal TotalPrice { get; set; }
}

public class OrderStatusChangedEvent
{
	public int OrderId { get; set; }
	public string OrderNumber { get; set; } = string.Empty;
	public OrderStatus OldStatus { get; set; }
	public OrderStatus NewStatus { get; set; }
	public int UserId { get; set; }
	public string UserEmail { get; set; } = string.Empty;
	public string UserFullName { get; set; } = string.Empty;
	public DateTime ChangedAt { get; set; }
}

public class ProductStockUpdatedEvent
{
	public int ProductId { get; set; }
	public string ProductName { get; set; } = string.Empty;
	public int OldStock { get; set; }
	public int NewStock { get; set; }
	public DateTime UpdatedAt { get; set; }
}