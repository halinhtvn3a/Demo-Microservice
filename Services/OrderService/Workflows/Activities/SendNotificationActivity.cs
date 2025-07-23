using Dapr.Client;
using Dapr.Workflow;
using OrderService.Workflows;

namespace OrderService.Workflows.Activities;

public class SendNotificationActivity : WorkflowActivity<NotificationInput, bool>
{
	private readonly DaprClient _daprClient;
	private readonly ILogger<SendNotificationActivity> _logger;

	public SendNotificationActivity(DaprClient daprClient, ILogger<SendNotificationActivity> logger)
	{
		_daprClient = daprClient;
		_logger = logger;
	}

	public override async Task<bool> RunAsync(WorkflowActivityContext context, NotificationInput input)
	{
		try
		{
			_logger.LogInformation("Sending notification '{Type}' for order {OrderId} to user {UserId}",
				input.Type, input.OrderId, input.UserId);


			var topicName = "order.notifications";
			await _daprClient.PublishEventAsync("pubsub", topicName, input);

			_logger.LogInformation("Notification published for order {OrderId}", input.OrderId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to publish notification for order {OrderId}", input.OrderId);
			return false;
		}
	}
}