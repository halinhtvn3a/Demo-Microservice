using Dapr.Workflow;
using Shared.Models;
using Shared.Models.DTOs;
using Shared.Models.Events;

namespace OrderService.Workflows;

public class OrderProcessingWorkflow : Workflow<OrderProcessingInput, OrderProcessingResult>
{
	public override async Task<OrderProcessingResult> RunAsync(WorkflowContext context, OrderProcessingInput input)
	{
		var logger = context.CreateReplaySafeLogger<OrderProcessingWorkflow>();
		logger.LogInformation("Starting order processing workflow for order {OrderId}", input.OrderId);

		try
		{
			// Step 1: Validate order
			var validationResult = await context.CallActivityAsync<bool>(
				nameof(ValidateOrderActivity),
				input);

			if (!validationResult)
			{
				logger.LogWarning("Order validation failed for order {OrderId}", input.OrderId);
				return new OrderProcessingResult
				{
					Success = false,
					Message = "Order validation failed",
					FinalStatus = OrderStatus.Cancelled
				};
			}

			// Step 2: Reserve inventory (parallel calls)
			var reservationTasks = input.Items.Select(item =>
				context.CallActivityAsync<bool>(
					nameof(ReserveInventoryActivity),
					new InventoryReservationInput
					{
						ProductId = item.ProductId,
						Quantity = item.Quantity,
						OrderId = input.OrderId
					})).ToList();

			var reservationResults = await Task.WhenAll(reservationTasks);

			if (reservationResults.Any(r => !r))
			{
				logger.LogWarning("Inventory reservation failed for order {OrderId}", input.OrderId);

				// Compensate: Release any successfully reserved inventory
				await CompensateInventoryReservations(context, input);

				return new OrderProcessingResult
				{
					Success = false,
					Message = "Insufficient inventory",
					FinalStatus = OrderStatus.Cancelled
				};
			}

			// Step 3: Update order status to confirmed
			await context.CallActivityAsync(
				nameof(UpdateOrderStatusActivity),
				new OrderStatusUpdate
				{
					OrderId = input.OrderId,
					Status = OrderStatus.Confirmed
				});

			// Step 4: Wait for external approval (if amount > threshold)
			if (input.TotalAmount > 1000)
			{
				logger.LogInformation("Order {OrderId} requires approval due to high value", input.OrderId);

				// Wait for approval event or timeout (30 minutes)
				var approvalReceived = await context.WaitForExternalEventAsync<bool>(
					"OrderApproval",
					TimeSpan.FromMinutes(30));

				if (!approvalReceived)
				{
					logger.LogWarning("Order {OrderId} approval timeout", input.OrderId);
					await CompensateInventoryReservations(context, input);

					return new OrderProcessingResult
					{
						Success = false,
						Message = "Order approval timeout",
						FinalStatus = OrderStatus.Cancelled
					};
				}
			}

			// Step 5: Process payment (simulated)
			var paymentResult = await context.CallActivityAsync<bool>(
				nameof(ProcessPaymentActivity),
				input);

			if (!paymentResult)
			{
				logger.LogWarning("Payment processing failed for order {OrderId}", input.OrderId);
				await CompensateInventoryReservations(context, input);

				return new OrderProcessingResult
				{
					Success = false,
					Message = "Payment processing failed",
					FinalStatus = OrderStatus.Cancelled
				};
			}

			// Step 6: Update order status to processing
			await context.CallActivityAsync(
				nameof(UpdateOrderStatusActivity),
				new OrderStatusUpdate
				{
					OrderId = input.OrderId,
					Status = OrderStatus.Processing
				});

			// Step 7: Send notifications
			await context.CallActivityAsync(
				nameof(SendNotificationActivity),
				new NotificationInput
				{
					OrderId = input.OrderId,
					UserId = input.UserId,
					Type = "OrderConfirmed"
				});

			// Step 8: Schedule shipping (with delay)
			var shippingDate = DateTime.UtcNow.AddDays(1);
			await context.CreateTimer(shippingDate, CancellationToken.None);

			// Step 9: Process shipping
			await context.CallActivityAsync(
				nameof(ProcessShippingActivity),
				input);

			// Step 10: Final status update
			await context.CallActivityAsync(
				nameof(UpdateOrderStatusActivity),
				new OrderStatusUpdate
				{
					OrderId = input.OrderId,
					Status = OrderStatus.Shipped
				});

			// Step 11: Send final notification
			await context.CallActivityAsync(
				nameof(SendNotificationActivity),
				new NotificationInput
				{
					OrderId = input.OrderId,
					UserId = input.UserId,
					Type = "OrderShipped"
				});

			logger.LogInformation("Order processing workflow completed successfully for order {OrderId}", input.OrderId);

			return new OrderProcessingResult
			{
				Success = true,
				Message = "Order processed successfully",
				FinalStatus = OrderStatus.Shipped
			};
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error in order processing workflow for order {OrderId}", input.OrderId);

			// Compensate any changes
			await CompensateInventoryReservations(context, input);

			return new OrderProcessingResult
			{
				Success = false,
				Message = $"Workflow error: {ex.Message}",
				FinalStatus = OrderStatus.Cancelled
			};
		}
	}

	private async Task CompensateInventoryReservations(WorkflowContext context, OrderProcessingInput input)
	{
		var logger = context.CreateReplaySafeLogger<OrderProcessingWorkflow>();
		logger.LogInformation("Starting inventory compensation for order {OrderId}", input.OrderId);

		var compensationTasks = input.Items.Select(item =>
			context.CallActivityAsync(
				nameof(ReleaseInventoryActivity),
				new InventoryReservationInput
				{
					ProductId = item.ProductId,
					Quantity = item.Quantity,
					OrderId = input.OrderId
				})).ToList();

		await Task.WhenAll(compensationTasks);

		logger.LogInformation("Inventory compensation completed for order {OrderId}", input.OrderId);
	}
}

// Workflow input/output models
public class OrderProcessingInput
{
	public int OrderId { get; set; }
	public int UserId { get; set; }
	public decimal TotalAmount { get; set; }
	public List<OrderItemInput> Items { get; set; } = new();
}

public class OrderItemInput
{
	public int ProductId { get; set; }
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
}

public class OrderProcessingResult
{
	public bool Success { get; set; }
	public string Message { get; set; } = string.Empty;
	public OrderStatus FinalStatus { get; set; }
}

public class InventoryReservationInput
{
	public int ProductId { get; set; }
	public int Quantity { get; set; }
	public int OrderId { get; set; }
}

public class OrderStatusUpdate
{
	public int OrderId { get; set; }
	public OrderStatus Status { get; set; }
}

public class NotificationInput
{
	public int OrderId { get; set; }
	public int UserId { get; set; }
	public string Type { get; set; } = string.Empty;
}