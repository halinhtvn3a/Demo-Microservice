using Refit;
using Shared.Models.DTOs;

namespace OrderService.Clients;

public interface IProductServiceClient
{
	[Get("/api/products/{id}")]
	Task<ProductDto> GetProductAsync(int id);

	[Get("/api/products/{id}/stock/check")]
	Task<object> CheckStockAsync(int id, [Query] int quantity);

	[Patch("/api/products/{id}/stock")]
	Task<object> UpdateStockAsync(int id, [Body] int quantityChange, [Authorize("Bearer")] string token);

	[Get("/api/products/health")]
	Task<object> GetHealthAsync();
}