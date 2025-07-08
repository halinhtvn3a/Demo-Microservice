namespace NotificationService.Services;

public class NotificationProcessorService : BackgroundService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<NotificationProcessorService> _logger;

	public NotificationProcessorService(
		IServiceProvider serviceProvider,
		ILogger<NotificationProcessorService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("Notification Processor Service started");

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var scope = _serviceProvider.CreateScope();
				var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

				// Process pending notifications every 30 seconds
				await notificationService.ProcessPendingNotificationsAsync();

				// Retry failed notifications every 5 minutes
				if (DateTime.UtcNow.Minute % 5 == 0)
				{
					await notificationService.RetryFailedNotificationsAsync();
				}

				await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in notification processor");
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
			}
		}

		_logger.LogInformation("Notification Processor Service stopped");
	}
}