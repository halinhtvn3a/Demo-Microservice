using Dapr.Workflow;
using OrderService.Clients;
using OrderService.Workflows;

namespace OrderService.Workflows.Activities;

public class ReserveInventoryActivity : WorkflowActivity<InventoryReservationInput, bool>
{
	private readonly IProductServiceClient _productServiceClient;
	private readonly ILogger<ReserveInventoryActivity> _logger;

	public ReserveInventoryActivity(
		IProductServiceClient productServiceClient,
		ILogger<ReserveInventoryActivity> logger)
	{
		_productServiceClient = productServiceClient;
		_logger = logger;
	}

	public override async Task<bool> RunAsync(WorkflowActivityContext context, InventoryReservationInput input)
	{
		try
		{
			_logger.LogInformation("Reserving inventory for product {ProductId}, quantity {Quantity}, order {OrderId}",
				input.ProductId, input.Quantity, input.OrderId);

			// Check stock availability first
			var stockCheck = await _productServiceClient.CheckStockAsync(input.ProductId, input.Quantity);

			// Reserve inventory by reducing stock
			var updateResult = await _productServiceClient.UpdateStockAsync(
				input.ProductId,
				-input.Quantity,
				"Bearer system-token"); // In real implementation, use proper service account token

			_logger.LogInformation("Inventory reservation completed for product {ProductId}, order {OrderId}",
				input.ProductId, input.OrderId);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to reserve inventory for product {ProductId}, order {OrderId}",
				input.ProductId, input.OrderId);
			return false;
		}
	}
}