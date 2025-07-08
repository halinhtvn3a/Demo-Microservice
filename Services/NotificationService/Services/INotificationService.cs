using NotificationService.Models;

namespace NotificationService.Services;

public interface INotificationService
{
	// Send notifications
	Task<bool> SendNotificationAsync(int userId, string type, Dictionary<string, object> parameters, NotificationChannel channel = NotificationChannel.Email);
	Task<bool> SendEmailAsync(string recipient, string subject, string message);
	Task<bool> SendSMSAsync(string phoneNumber, string message);

	// Notification management
	Task<Notification?> GetNotificationAsync(int id);
	Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 10);
	Task<IEnumerable<Notification>> GetNotificationsByStatusAsync(NotificationStatus status);
	Task<bool> MarkAsReadAsync(int notificationId);
	Task<bool> MarkAsReadAsync(List<int> notificationIds);

	// Template management
	Task<NotificationTemplate?> GetTemplateAsync(string type, NotificationChannel channel);
	Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync();
	Task<bool> CreateTemplateAsync(NotificationTemplate template);
	Task<bool> UpdateTemplateAsync(NotificationTemplate template);

	// Event handlers
	Task HandleOrderCreatedAsync(Shared.Models.Events.OrderCreatedEvent orderEvent);
	Task HandleOrderStatusUpdatedAsync(Shared.Models.Events.OrderStatusUpdatedEvent statusEvent);
	Task HandleOrderCancelledAsync(Shared.Models.Events.OrderCancelledEvent cancelEvent);
	Task HandleProductStockUpdatedAsync(Shared.Models.Events.ProductStockUpdatedEvent stockEvent);

	// Processing
	Task ProcessPendingNotificationsAsync();
	Task RetryFailedNotificationsAsync();
}