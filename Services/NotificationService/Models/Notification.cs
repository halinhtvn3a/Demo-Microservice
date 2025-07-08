namespace NotificationService.Models;

public class Notification
{
	public int Id { get; set; }
	public int UserId { get; set; }
	public string Type { get; set; } = string.Empty;
	public NotificationChannel Channel { get; set; }
	public string Recipient { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public NotificationStatus Status { get; set; }
	public string? ErrorMessage { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? SentAt { get; set; }
	public int? RelatedEntityId { get; set; }
	public string? RelatedEntityType { get; set; }
}

public class NotificationTemplate
{
	public int Id { get; set; }
	public string Type { get; set; } = string.Empty;
	public NotificationChannel Channel { get; set; }
	public string Subject { get; set; } = string.Empty;
	public string Template { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
}

public enum NotificationChannel
{
	Email,
	SMS,
	Push,
	Internal
}

public enum NotificationStatus
{
	Pending,
	Sent,
	Failed,
	Cancelled
}