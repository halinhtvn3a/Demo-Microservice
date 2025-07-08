using Shared.Models;
using Shared.Models.DTOs;

namespace ProductService.Services;

public interface IProductService
{
	// REST API methods
	Task<ProductDto?> GetProductByIdAsync(int id);
	Task<IEnumerable<ProductDto>> GetProductsAsync(ProductSearchRequest request);
	Task<ProductDto?> CreateProductAsync(CreateProductRequest request);
	Task<ProductDto?> UpdateProductAsync(int id, UpdateProductRequest request);
	Task<bool> DeleteProductAsync(int id);

	// Stock management
	Task<bool> CheckStockAsync(int productId, int requiredQuantity);
	Task<bool> UpdateStockAsync(int productId, int quantityChange);
	Task<int> GetCurrentStockAsync(int productId);

	// Cache management
	Task InvalidateCacheAsync(int productId);
	Task InvalidateListCacheAsync();

	// Events
	Task PublishStockUpdatedEventAsync(Product product, int oldStock, int newStock);
}