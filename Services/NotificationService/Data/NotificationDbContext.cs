using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data;

public class NotificationDbContext : DbContext
{
	public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
	{
	}

	public DbSet<Notification> Notifications { get; set; }
	public DbSet<NotificationTemplate> NotificationTemplates { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Notification>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasIndex(e => e.UserId);
			entity.HasIndex(e => e.Type);
			entity.HasIndex(e => e.Status);
			entity.HasIndex(e => e.CreatedAt);

			entity.Property(e => e.Subject)
				.IsRequired()
				.HasMaxLength(200);

			entity.Property(e => e.Message)
				.IsRequired()
				.HasMaxLength(2000);

			entity.Property(e => e.Type)
				.IsRequired()
				.HasMaxLength(50);

			entity.Property(e => e.Status)
				.IsRequired()
				.HasConversion<string>();

			entity.Property(e => e.Channel)
				.IsRequired()
				.HasConversion<string>();

			entity.Property(e => e.Recipient)
				.IsRequired()
				.HasMaxLength(200);

			entity.Property(e => e.ErrorMessage)
				.HasMaxLength(500);
		});

		modelBuilder.Entity<NotificationTemplate>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasIndex(e => new { e.Type, e.Channel }).IsUnique();

			entity.Property(e => e.Type)
				.IsRequired()
				.HasMaxLength(50);

			entity.Property(e => e.Channel)
				.IsRequired()
				.HasConversion<string>();

			entity.Property(e => e.Subject)
				.IsRequired()
				.HasMaxLength(200);

			entity.Property(e => e.Template)
				.IsRequired()
				.HasMaxLength(5000);
		});

		// Seed notification templates
		modelBuilder.Entity<NotificationTemplate>().HasData(
			new NotificationTemplate
			{
				Id = 1,
				Type = "OrderConfirmed",
				Channel = NotificationChannel.Email,
				Subject = "Order Confirmed - #{OrderNumber}",
				Template = "Dear {UserName},\n\nYour order #{OrderNumber} has been confirmed.\nTotal Amount: ${TotalAmount}\n\nThank you for your business!",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			},
			new NotificationTemplate
			{
				Id = 2,
				Type = "OrderShipped",
				Channel = NotificationChannel.Email,
				Subject = "Order Shipped - #{OrderNumber}",
				Template = "Dear {UserName},\n\nYour order #{OrderNumber} has been shipped.\nTracking Number: {TrackingNumber}\n\nYou can track your order at our website.",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			},
			new NotificationTemplate
			{
				Id = 3,
				Type = "OrderCancelled",
				Channel = NotificationChannel.Email,
				Subject = "Order Cancelled - #{OrderNumber}",
				Template = "Dear {UserName},\n\nYour order #{OrderNumber} has been cancelled.\nReason: {Reason}\n\nIf you have any questions, please contact our support team.",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			},
			new NotificationTemplate
			{
				Id = 4,
				Type = "ProductStockUpdated",
				Channel = NotificationChannel.Internal,
				Subject = "Product Stock Updated - {ProductName}",
				Template = "Product: {ProductName}\nOld Stock: {OldStock}\nNew Stock: {NewStock}\nUpdated At: {UpdatedAt}",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			}
		);
	}
}