using Dapr.Workflow;
using OrderService.Workflows;

namespace OrderService.Workflows.Activities;

public class ProcessShippingActivity : WorkflowActivity<OrderProcessingInput, bool>
{
	private readonly ILogger<ProcessShippingActivity> _logger;

	public ProcessShippingActivity(ILogger<ProcessShippingActivity> logger)
	{
		_logger = logger;
	}

	public override async Task<bool> RunAsync(WorkflowActivityContext context, OrderProcessingInput input)
	{
		try
		{
			_logger.LogInformation("Processing shipping for order {OrderId}", input.OrderId);

			// Simulate shipping process
			await Task.Delay(2000);

			_logger.LogInformation("Shipping processed for order {OrderId}", input.OrderId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing shipping for order {OrderId}", input.OrderId);
			return false;
		}
	}
}