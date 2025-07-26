namespace Shared.Events;

public record SendEmailNotificationEvent(
    string To,
    string Subject,
    string Body,
    string? From = null
);

public record SendSmsNotificationEvent(
    string PhoneNumber,
    string Message
);

public record UserRegisteredEvent(
    int UserId,
    string Email,
    string FullName,
    DateTime RegisteredAt
);

public record PasswordResetRequestedEvent(
    int UserId,
    string Email,
    string ResetToken,
    DateTime RequestedAt
);

public record ProductStockUpdatedEvent(
    int ProductId,
    string ProductName,
    int OldStock,
    int NewStock,
    DateTime UpdatedAt
);