using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.DTOs;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products with optional filtering
    /// </summary>
    /// <param name="request">Search and filter criteria</param>
    /// <returns>List of products</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), 200)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductSearchRequest request)
    {
        var products = await _productService.GetProductsAsync(request);
        return Ok(products);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var product = await _productService.CreateProductAsync(request);
        if (product == null)
        {
            return BadRequest(new { message = "Failed to create product" });
        }

        _logger.LogInformation("Product {ProductId} created by user", product.Id);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var product = await _productService.UpdateProductAsync(id, request);
        if (product == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Product {ProductId} updated by user", id);
        return Ok(product);
    }


    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var success = await _productService.DeleteProductAsync(id);
        if (!success)
        {
            return NotFound();
        }

        _logger.LogInformation("Product {ProductId} deleted by user", id);
        return NoContent();
    }


    [HttpGet("{id}/stock/check")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CheckStock(int id, [FromQuery] int quantity = 1)
    {
        var available = await _productService.CheckStockAsync(id, quantity);
        var currentStock = await _productService.GetCurrentStockAsync(id);

        return Ok(new
        {
            productId = id,
            requiredQuantity = quantity,
            currentStock,
            available,
            message = available ? "Stock available" : "Insufficient stock"
        });
    }


    [HttpPatch("{id}/stock")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] int quantityChange)
    {
        var success = await _productService.UpdateStockAsync(id, quantityChange);
        if (!success)
        {
            return BadRequest(new { message = "Failed to update stock. Product not found or insufficient stock." });
        }

        var newStock = await _productService.GetCurrentStockAsync(id);

        _logger.LogInformation("Stock updated for product {ProductId} by {Change}, new stock: {NewStock}",
            id, quantityChange, newStock);

        return Ok(new
        {
            productId = id,
            quantityChange,
            newStock,
            message = "Stock updated successfully"
        });
    }


    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), 200)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "ProductService",
            timestamp = DateTime.UtcNow
        });
    }
}