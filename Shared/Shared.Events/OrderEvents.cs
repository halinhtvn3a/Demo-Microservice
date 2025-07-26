namespace Shared.Events;

public record OrderCreatedEvent(
    int OrderId,
    int UserId,
    decimal TotalAmount,
    List<OrderItemEvent> Items,
    DateTime CreatedAt
);

public record OrderItemEvent(
    int ProductId,
    int Quantity,
    decimal Price
);

public record OrderStatusChangedEvent(
    int OrderId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt
);

public record OrderCompletedEvent(
    int OrderId,
    int UserId,
    decimal TotalAmount,
    DateTime CompletedAt
);

public record OrderCancelledEvent(
    int OrderId,
    int UserId,
    string Reason,
    DateTime CancelledAt
);