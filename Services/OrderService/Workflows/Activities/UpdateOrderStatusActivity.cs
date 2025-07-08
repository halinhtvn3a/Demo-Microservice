using Dapr.Workflow;
using OrderService.Data;
using OrderService.Workflows;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Workflows.Activities;

public class UpdateOrderStatusActivity : WorkflowActivity<OrderStatusUpdate, bool>
{
	private readonly OrderDbContext _context;
	private readonly ILogger<UpdateOrderStatusActivity> _logger;

	public UpdateOrderStatusActivity(
		OrderDbContext context,
		ILogger<UpdateOrderStatusActivity> logger)
	{
		_context = context;
		_logger = logger;
	}

	public override async Task<bool> RunAsync(WorkflowActivityContext context, OrderStatusUpdate input)
	{
		try
		{
			_logger.LogInformation("Updating order {OrderId} status to {Status}",
				input.OrderId, input.Status);

			var order = await _context.Orders.FindAsync(input.OrderId);
			if (order == null)
			{
				_logger.LogWarning("Order {OrderId} not found for status update", input.OrderId);
				return false;
			}

			order.Status = input.Status;
			order.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			_logger.LogInformation("Order {OrderId} status updated to {Status}",
				input.OrderId, input.Status);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating order {OrderId} status", input.OrderId);
			return false;
		}
	}
}