using AutoMapper;
using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Shared.Models;
using Shared.Models.DTOs;
using Shared.Models.Events;
using ProductService.Data;

namespace ProductService.Services;

public class ProductServiceImpl : IProductService
{
	private readonly ProductDbContext _context;
	private readonly IMapper _mapper;
	private readonly HybridCache _cache;
	private readonly DaprClient _daprClient;
	private readonly ILogger<ProductServiceImpl> _logger;

	private const string PRODUCT_CACHE_KEY = "product:";
	private const string PRODUCTS_LIST_CACHE_KEY = "products:list:";
	private const int CACHE_DURATION_MINUTES = 15;

	public ProductServiceImpl(
		ProductDbContext context,
		IMapper mapper,
		HybridCache cache,
		DaprClient daprClient,
		ILogger<ProductServiceImpl> logger)
	{
		_context = context;
		_mapper = mapper;
		_cache = cache;
		_daprClient = daprClient;
		_logger = logger;
	}

	public async Task<ProductDto?> GetProductByIdAsync(int id)
	{
		try
		{
			var cacheKey = $"{PRODUCT_CACHE_KEY}{id}";

			// Try cache first
			var cachedProduct = await _cache.GetOrCreateAsync<ProductDto>(cacheKey, _ => ValueTask.FromResult<ProductDto?>(null), new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableUnderlyingData | HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite });
			if (cachedProduct != null)
			{
				_logger.LogInformation("Product {ProductId} retrieved from cache", id);
				return cachedProduct;
			}

			// Get from database
			var product = await _context.Products
				.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

			if (product == null)
			{
				_logger.LogWarning("Product {ProductId} not found", id);
				return null;
			}

			var productDto = _mapper.Map<ProductDto>(product);

			// Cache for future requests
			await _cache.SetAsync(cacheKey, productDto, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES) });

			_logger.LogInformation("Product {ProductId} retrieved from database and cached", id);
			return productDto;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting product by ID: {ProductId}", id);
			return null;
		}
	}

	public async Task<IEnumerable<ProductDto>> GetProductsAsync(ProductSearchRequest request)
	{
		try
		{
			var cacheKey = $"{PRODUCTS_LIST_CACHE_KEY}{GenerateCacheKey(request)}";

			// Try cache first
			var cachedProducts = await _cache.GetOrCreateAsync<IEnumerable<ProductDto>>(cacheKey, _ => ValueTask.FromResult<IEnumerable<ProductDto>?>(null), new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableUnderlyingData | HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite });
			if (cachedProducts != null)
			{
				_logger.LogInformation("Products list retrieved from cache");
				return cachedProducts;
			}

			// Build query
			var query = _context.Products.Where(p => p.IsActive);

			if (!string.IsNullOrEmpty(request.Name))
			{
				query = query.Where(p => p.Name.Contains(request.Name));
			}

			if (!string.IsNullOrEmpty(request.Category))
			{
				query = query.Where(p => p.Category.Contains(request.Category));
			}

			if (request.MinPrice.HasValue)
			{
				query = query.Where(p => p.Price >= (decimal)request.MinPrice.Value);
			}

			if (request.MaxPrice.HasValue)
			{
				query = query.Where(p => p.Price <= (decimal)request.MaxPrice.Value);
			}

			if (request.InStock.HasValue && request.InStock.Value)
			{
				query = query.Where(p => p.Stock > 0);
			}

			// Apply pagination
			var products = await query
				.OrderBy(p => p.Name)
				.Skip((request.Page - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);

			// Cache results
			await _cache.SetAsync(cacheKey, productDtos, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES) });

			_logger.LogInformation("Retrieved {Count} products from database and cached", products.Count);
			return productDtos;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting products with search criteria");
			return Enumerable.Empty<ProductDto>();
		}
	}

	public async Task<ProductDto?> CreateProductAsync(CreateProductRequest request)
	{
		try
		{
			var product = _mapper.Map<Product>(request);

			_context.Products.Add(product);
			await _context.SaveChangesAsync();

			var productDto = _mapper.Map<ProductDto>(product);

			// Cache the new product
			var cacheKey = $"{PRODUCT_CACHE_KEY}{product.Id}";
			await _cache.SetAsync(cacheKey, productDto, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES) });

			// Invalidate list cache
			await InvalidateListCacheAsync();

			_logger.LogInformation("Product {ProductId} created successfully", product.Id);
			return productDto;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error creating product");
			return null;
		}
	}

	public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request)
	{
		try
		{
			var product = await _context.Products
				.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

			if (product == null)
			{
				_logger.LogWarning("Product {ProductId} not found for update", id);
				return null;
			}

			var oldStock = product.Stock;

			// Apply updates
			_mapper.Map(request, product);
			product.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			var productDto = _mapper.Map<ProductDto>(product);

			// Update cache
			await InvalidateCacheAsync(id);
			await InvalidateListCacheAsync();

			// Publish stock updated event if stock changed
			if (oldStock != product.Stock)
			{
				await PublishStockUpdatedEventAsync(product, oldStock, product.Stock);
			}

			_logger.LogInformation("Product {ProductId} updated successfully", id);
			return productDto;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating product {ProductId}", id);
			return null;
		}
	}

	public async Task<bool> DeleteProductAsync(int id)
	{
		try
		{
			var product = await _context.Products
				.FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

			if (product == null)
			{
				_logger.LogWarning("Product {ProductId} not found for deletion", id);
				return false;
			}

			// Soft delete
			product.IsActive = false;
			product.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			// Invalidate cache
			await InvalidateCacheAsync(id);
			await InvalidateListCacheAsync();

			_logger.LogInformation("Product {ProductId} deleted successfully", id);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error deleting product {ProductId}", id);
			return false;
		}
	}

	public async Task<bool> CheckStockAsync(int productId, int requiredQuantity)
	{
		try
		{
			var product = await GetProductEntityAsync(productId);
			if (product == null)
			{
				_logger.LogWarning("Product {ProductId} not found for stock check", productId);
				return false;
			}

			var available = product.Stock >= requiredQuantity;
			_logger.LogInformation("Stock check for product {ProductId}: Required={Required}, Available={Available}, Result={Result}",
				productId, requiredQuantity, product.Stock, available);

			return available;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error checking stock for product {ProductId}", productId);
			return false;
		}
	}

	public async Task<bool> UpdateStockAsync(int productId, int quantityChange)
	{
		try
		{
			var product = await _context.Products
				.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

			if (product == null)
			{
				_logger.LogWarning("Product {ProductId} not found for stock update", productId);
				return false;
			}

			var oldStock = product.Stock;
			var newStock = oldStock + quantityChange;

			if (newStock < 0)
			{
				_logger.LogWarning("Insufficient stock for product {ProductId}. Current: {Current}, Change: {Change}",
					productId, oldStock, quantityChange);
				return false;
			}

			product.Stock = newStock;
			product.UpdatedAt = DateTime.UtcNow;

			await _context.SaveChangesAsync();

			// Invalidate cache
			await InvalidateCacheAsync(productId);
			await InvalidateListCacheAsync();

			// Publish stock updated event
			await PublishStockUpdatedEventAsync(product, oldStock, newStock);

			_logger.LogInformation("Stock updated for product {ProductId}: {OldStock} -> {NewStock}",
				productId, oldStock, newStock);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
			return false;
		}
	}

	public async Task<int> GetCurrentStockAsync(int productId)
	{
		try
		{
			var product = await GetProductEntityAsync(productId);
			return product?.Stock ?? 0;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting current stock for product {ProductId}", productId);
			return 0;
		}
	}

	public async Task InvalidateCacheAsync(int productId)
	{
		try
		{
			var cacheKey = $"{PRODUCT_CACHE_KEY}{productId}";
			await _cache.RemoveAsync(cacheKey);
			_logger.LogDebug("Cache invalidated for product {ProductId}", productId);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error invalidating cache for product {ProductId}", productId);
		}
	}

	public async Task InvalidateListCacheAsync()
	{
		try
		{
			// In a real implementation, you might want to use cache tags or patterns
			// For now, we'll just log this action
			_logger.LogDebug("Product list cache invalidation requested");
			await Task.CompletedTask;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error invalidating list cache");
		}
	}

	public async Task PublishStockUpdatedEventAsync(Product product, int oldStock, int newStock)
	{
		try
		{
			var stockEvent = _mapper.Map<ProductStockUpdatedEvent>(product);
			stockEvent.OldStock = oldStock;
			stockEvent.NewStock = newStock;

			await _daprClient.PublishEventAsync("pubsub", "product-stock-updated", stockEvent);

			_logger.LogInformation("Published stock updated event for product {ProductId}", product.Id);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error publishing stock updated event for product {ProductId}", product.Id);
		}
	}

	private async Task<Product?> GetProductEntityAsync(int productId)
	{
		return await _context.Products
			.FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);
	}

	private static string GenerateCacheKey(ProductSearchRequest request)
	{
		return $"{request.Name ?? "all"}_{request.Category ?? "all"}_{request.MinPrice}_{request.MaxPrice}_{request.InStock}_{request.Page}_{request.PageSize}";
	}
}