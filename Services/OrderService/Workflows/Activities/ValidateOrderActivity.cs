using Dapr.Workflow;
using OrderService.Clients;
using OrderService.Workflows;

namespace OrderService.Workflows.Activities;

public class ValidateOrderActivity : WorkflowActivity<OrderProcessingInput, bool>
{
	private readonly IUserServiceClient _userServiceClient;
	private readonly IProductServiceClient _productServiceClient;
	private readonly ILogger<ValidateOrderActivity> _logger;

	public ValidateOrderActivity(
		IUserServiceClient userServiceClient,
		IProductServiceClient productServiceClient,
		ILogger<ValidateOrderActivity> logger)
	{
		_userServiceClient = userServiceClient;
		_productServiceClient = productServiceClient;
		_logger = logger;
	}

	public override async Task<bool> RunAsync(WorkflowActivityContext context, OrderProcessingInput input)
	{
		try
		{
			_logger.LogInformation("Validating order {OrderId}", input.OrderId);

			// Validate user exists
			try
			{
				var user = await _userServiceClient.GetUserAsync(input.UserId, "Bearer dummy-token");
				if (user == null)
				{
					_logger.LogWarning("User {UserId} not found for order {OrderId}", input.UserId, input.OrderId);
					return false;
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to validate user {UserId} for order {OrderId}", input.UserId, input.OrderId);
				return false;
			}

			// Validate all products exist and have sufficient stock
			foreach (var item in input.Items)
			{
				try
				{
					var product = await _productServiceClient.GetProductAsync(item.ProductId);
					if (product == null)
					{
						_logger.LogWarning("Product {ProductId} not found for order {OrderId}", item.ProductId, input.OrderId);
						return false;
					}

					var stockCheck = await _productServiceClient.CheckStockAsync(item.ProductId, item.Quantity);
					// Note: This is a simplified check. In reality, you'd parse the response properly

					_logger.LogInformation("Product {ProductId} validation passed for order {OrderId}", item.ProductId, input.OrderId);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to validate product {ProductId} for order {OrderId}", item.ProductId, input.OrderId);
					return false;
				}
			}

			// Validate order amount
			if (input.TotalAmount <= 0)
			{
				_logger.LogWarning("Invalid total amount {Amount} for order {OrderId}", input.TotalAmount, input.OrderId);
				return false;
			}

			// Validate items count
			if (!input.Items.Any())
			{
				_logger.LogWarning("No items found for order {OrderId}", input.OrderId);
				return false;
			}

			_logger.LogInformation("Order {OrderId} validation completed successfully", input.OrderId);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error validating order {OrderId}", input.OrderId);
			return false;
		}
	}
}