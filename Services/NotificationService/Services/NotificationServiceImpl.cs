using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using Shared.Models.Events;
using System.Text.Json;

namespace NotificationService.Services;

public class NotificationServiceImpl : INotificationService
{
	private readonly NotificationDbContext _context;
	private readonly IEmailService _emailService;
	private readonly ILogger<NotificationServiceImpl> _logger;

	public NotificationServiceImpl(
		NotificationDbContext context,
		IEmailService emailService,
		ILogger<NotificationServiceImpl> logger)
	{
		_context = context;
		_emailService = emailService;
		_logger = logger;
	}

	public async Task<bool> SendNotificationAsync(int userId, string type, Dictionary<string, object> parameters, NotificationChannel channel = NotificationChannel.Email)
	{
		try
		{
			var template = await GetTemplateAsync(type, channel);
			if (template == null)
			{
				_logger.LogWarning("No template found for type: {Type}, channel: {Channel}", type, channel);
				return false;
			}

			var subject = ProcessTemplate(template.Subject, parameters);
			var message = ProcessTemplate(template.Template, parameters);
			var recipient = parameters.GetValueOrDefault("Email", "user@demo.com").ToString();

			var notification = new Notification
			{
				UserId = userId,
				Type = type,
				Channel = channel,
				Recipient = recipient!,
				Subject = subject,
				Message = message,
				Status = NotificationStatus.Pending,
				CreatedAt = DateTime.UtcNow
			};

			_context.Notifications.Add(notification);
			await _context.SaveChangesAsync();

			// Send immediately for demo
			var success = await SendEmailAsync(recipient!, subject, message);

			notification.Status = success ? NotificationStatus.Sent : NotificationStatus.Failed;
			notification.SentAt = success ? DateTime.UtcNow : null;

			await _context.SaveChangesAsync();

			return success;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending notification");
			return false;
		}
	}

	public async Task<bool> SendEmailAsync(string recipient, string subject, string message)
	{
		return await _emailService.SendEmailAsync(recipient, subject, message);
	}

	public async Task<bool> SendSMSAsync(string phoneNumber, string message)
	{
		// Mock SMS sending
		_logger.LogInformation("SMS sent to {Phone}: {Message}", phoneNumber, message);
		await Task.Delay(50);
		return true;
	}

	public async Task<Notification?> GetNotificationAsync(int id)
	{
		return await _context.Notifications.FindAsync(id);
	}

	public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 10)
	{
		return await _context.Notifications
			.Where(n => n.UserId == userId)
			.OrderByDescending(n => n.CreatedAt)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();
	}

	public async Task<IEnumerable<Notification>> GetNotificationsByStatusAsync(NotificationStatus status)
	{
		return await _context.Notifications
			.Where(n => n.Status == status)
			.OrderBy(n => n.CreatedAt)
			.ToListAsync();
	}

	public async Task<bool> MarkAsReadAsync(int notificationId)
	{
		var notification = await _context.Notifications.FindAsync(notificationId);
		if (notification != null)
		{
			// In a real implementation, you'd have a "read" status
			_logger.LogInformation("Notification {Id} marked as read", notificationId);
			return true;
		}
		return false;
	}

	public async Task<bool> MarkAsReadAsync(List<int> notificationIds)
	{
		var tasks = notificationIds.Select(MarkAsReadAsync);
		var results = await Task.WhenAll(tasks);
		return results.All(r => r);
	}

	public async Task<NotificationTemplate?> GetTemplateAsync(string type, NotificationChannel channel)
	{
		return await _context.NotificationTemplates
			.FirstOrDefaultAsync(t => t.Type == type && t.Channel == channel && t.IsActive);
	}

	public async Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync()
	{
		return await _context.NotificationTemplates
			.Where(t => t.IsActive)
			.OrderBy(t => t.Type)
			.ToListAsync();
	}

	public async Task<bool> CreateTemplateAsync(NotificationTemplate template)
	{
		try
		{
			template.CreatedAt = DateTime.UtcNow;
			_context.NotificationTemplates.Add(template);
			await _context.SaveChangesAsync();
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating template");
			return false;
		}
	}

	public async Task<bool> UpdateTemplateAsync(NotificationTemplate template)
	{
		try
		{
			template.UpdatedAt = DateTime.UtcNow;
			_context.NotificationTemplates.Update(template);
			await _context.SaveChangesAsync();
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating template");
			return false;
		}
	}

	// Event Handlers
	public async Task HandleOrderCreatedAsync(OrderCreatedEvent orderEvent)
	{
		var parameters = new Dictionary<string, object>
		{
			{"OrderNumber", orderEvent.OrderNumber},
			{"TotalAmount", orderEvent.TotalAmount},
			{"UserName", "Customer"}, // In real implementation, fetch from UserService
            {"Email", "customer@demo.com"} // In real implementation, fetch from UserService
        };

		await SendNotificationAsync(orderEvent.UserId, "OrderConfirmed", parameters);
		_logger.LogInformation("Order created notification sent for order {OrderNumber}", orderEvent.OrderNumber);
	}

	public async Task HandleOrderStatusUpdatedAsync(OrderStatusUpdatedEvent statusEvent)
	{
		if (statusEvent.NewStatus.ToString() == "Shipped")
		{
			var parameters = new Dictionary<string, object>
			{
				{"OrderNumber", statusEvent.OrderNumber},
				{"TrackingNumber", "TRK" + DateTime.Now.Ticks},
				{"UserName", "Customer"},
				{"Email", "customer@demo.com"}
			};

			await SendNotificationAsync(1, "OrderShipped", parameters); // Mock user ID
			_logger.LogInformation("Order shipped notification sent for order {OrderNumber}", statusEvent.OrderNumber);
		}
	}

	public async Task HandleOrderCancelledAsync(OrderCancelledEvent cancelEvent)
	{
		var parameters = new Dictionary<string, object>
		{
			{"OrderNumber", cancelEvent.OrderNumber},
			{"Reason", cancelEvent.Reason},
			{"UserName", "Customer"},
			{"Email", "customer@demo.com"}
		};

		await SendNotificationAsync(cancelEvent.UserId, "OrderCancelled", parameters);
		_logger.LogInformation("Order cancelled notification sent for order {OrderNumber}", cancelEvent.OrderNumber);
	}

	public async Task HandleProductStockUpdatedAsync(ProductStockUpdatedEvent stockEvent)
	{
		var parameters = new Dictionary<string, object>
		{
			{"ProductName", stockEvent.ProductName},
			{"OldStock", stockEvent.OldStock},
			{"NewStock", stockEvent.NewStock},
			{"UpdatedAt", stockEvent.UpdatedAt},
			{"Email", "admin@demo.com"}
		};

		await SendNotificationAsync(1, "ProductStockUpdated", parameters, NotificationChannel.Internal);
		_logger.LogInformation("Product stock update notification sent for {ProductName}", stockEvent.ProductName);
	}

	public async Task ProcessPendingNotificationsAsync()
	{
		var pending = await GetNotificationsByStatusAsync(NotificationStatus.Pending);
		_logger.LogInformation("Processing {Count} pending notifications", pending.Count());

		foreach (var notification in pending)
		{
			var success = await SendEmailAsync(notification.Recipient, notification.Subject, notification.Message);
			notification.Status = success ? NotificationStatus.Sent : NotificationStatus.Failed;
			notification.SentAt = success ? DateTime.UtcNow : null;
		}

		await _context.SaveChangesAsync();
	}

	public async Task RetryFailedNotificationsAsync()
	{
		var failed = await GetNotificationsByStatusAsync(NotificationStatus.Failed);
		_logger.LogInformation("Retrying {Count} failed notifications", failed.Count());

		foreach (var notification in failed)
		{
			var success = await SendEmailAsync(notification.Recipient, notification.Subject, notification.Message);
			if (success)
			{
				notification.Status = NotificationStatus.Sent;
				notification.SentAt = DateTime.UtcNow;
			}
		}

		await _context.SaveChangesAsync();
	}

	private static string ProcessTemplate(string template, Dictionary<string, object> parameters)
	{
		var result = template;
		foreach (var param in parameters)
		{
			result = result.Replace($"{{{param.Key}}}", param.Value?.ToString() ?? "");
		}
		return result;
	}
}