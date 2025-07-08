using Dapr.Workflow;
using OrderService.Workflows;

namespace OrderService.Workflows.Activities;

public class ProcessPaymentActivity : WorkflowActivity<OrderProcessingInput, bool>
{
	private readonly ILogger<ProcessPaymentActivity> _logger;

	public ProcessPaymentActivity(ILogger<ProcessPaymentActivity> logger)
	{
		_logger = logger;
	}

	public override async Task<bool> RunAsync(WorkflowActivityContext context, OrderProcessingInput input)
	{
		try
		{
			_logger.LogInformation("Processing payment for order {OrderId}, amount {Amount}",
				input.OrderId, input.TotalAmount);

			// Simulate payment processing
			await Task.Delay(2000); // Simulate external payment gateway call

			// For demo purposes, randomly succeed/fail based on amount
			var success = input.TotalAmount < 5000; // Orders over $5000 may fail

			if (success)
			{
				_logger.LogInformation("Payment successful for order {OrderId}", input.OrderId);
			}
			else
			{
				_logger.LogWarning("Payment failed for order {OrderId} - amount too high", input.OrderId);
			}

			return success;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing payment for order {OrderId}", input.OrderId);
			return false;
		}
	}
}