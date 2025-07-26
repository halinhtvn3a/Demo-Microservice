using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders/grpc")]
[Authorize]
public class OrderGrpcController : ControllerBase
{
    private readonly IOrderGrpcService _orderGrpcService;
    private readonly ILogger<OrderGrpcController> _logger;

    public OrderGrpcController(IOrderGrpcService orderGrpcService, ILogger<OrderGrpcController> logger)
    {
        _orderGrpcService = orderGrpcService;
        _logger = logger;
    }

    /// <summary>
    /// Validate order items using gRPC (high-performance stock checking)
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateOrderItems([FromBody] List<OrderItemValidationRequest> items)
    {
        try
        {
            var itemsToValidate = items.Select(i => (i.ProductId, i.Quantity)).ToList();
            var isValid = await _orderGrpcService.ValidateOrderItemsViaGrpcAsync(itemsToValidate);

            return Ok(new { valid = isValid, message = isValid ? "All items valid" : "Some items invalid" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order items via gRPC");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Reserve stock for an order using gRPC
    /// </summary>
    [HttpPost("{orderId}/reserve-stock")]
    public async Task<IActionResult> ReserveStock(int orderId)
    {
        try
        {
            var success = await _orderGrpcService.ReserveStockViaGrpcAsync(orderId);

            if (success)
            {
                return Ok(new { message = "Stock reserved successfully via gRPC" });
            }

            return BadRequest(new { error = "Failed to reserve stock" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for order {OrderId} via gRPC", orderId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Release stock for an order using gRPC
    /// </summary>
    [HttpPost("{orderId}/release-stock")]
    public async Task<IActionResult> ReleaseStock(int orderId)
    {
        try
        {
            var success = await _orderGrpcService.ReleaseStockViaGrpcAsync(orderId);

            if (success)
            {
                return Ok(new { message = "Stock released successfully via gRPC" });
            }

            return BadRequest(new { error = "Failed to release stock" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing stock for order {OrderId} via gRPC", orderId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class OrderItemValidationRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}