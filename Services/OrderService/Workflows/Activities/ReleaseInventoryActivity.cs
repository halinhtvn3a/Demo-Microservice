using Dapr.Workflow;
using OrderService.Clients;
using OrderService.Workflows;

namespace OrderService.Workflows.Activities;

public class ReleaseInventoryActivity : WorkflowActivity<InventoryReservationInput, bool>
{
	private readonly IProductServiceClient _productServiceClient;
	private readonly ILogger<ReleaseInventoryActivity> _logger;

	public ReleaseInventoryActivity(
		IProductServiceClient productServiceClient,
		ILogger<ReleaseInventoryActivity> logger)
	{
		_productServiceClient = productServiceClient;
		_logger = logger;
	}

	public override async Task<bool> RunAsync(WorkflowActivityContext context, InventoryReservationInput input)
	{
		try
		{
			_logger.LogInformation("Releasing inventory for product {ProductId}, quantity {Quantity}, order {OrderId}",
				input.ProductId, input.Quantity, input.OrderId);

			// Add stock back (compensation)
			await _productServiceClient.UpdateStockAsync(
				input.ProductId,
				input.Quantity, // Positive quantity to add stock back
				"Bearer system-token");

			_logger.LogInformation("Inventory release completed for product {ProductId}, order {OrderId}",
				input.ProductId, input.OrderId);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to release inventory for product {ProductId}, order {OrderId}",
				input.ProductId, input.OrderId);
			return false;
		}
	}
}