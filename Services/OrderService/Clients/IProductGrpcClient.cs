using OrderService.Protos;

namespace OrderService.Clients;

public interface IProductGrpcClient
{
    Task<ProductResponse> GetProductAsync(int productId);
    Task<CheckStockResponse> CheckStockAsync(int productId, int requiredQuantity);
    Task<UpdateStockResponse> UpdateStockAsync(int productId, int quantityChange);
}

public class ProductGrpcClient : IProductGrpcClient
{
    private readonly ProductGrpcService.ProductGrpcServiceClient _client;
    private readonly ILogger<ProductGrpcClient> _logger;

    public ProductGrpcClient(ProductGrpcService.ProductGrpcServiceClient client, ILogger<ProductGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<ProductResponse> GetProductAsync(int productId)
    {
        try
        {
            _logger.LogInformation("Getting product {ProductId} via gRPC", productId);

            var request = new GetProductRequest { Id = productId };
            var response = await _client.GetProductAsync(request);

            _logger.LogInformation("Successfully retrieved product {ProductId} via gRPC", productId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId} via gRPC", productId);
            throw;
        }
    }

    public async Task<CheckStockResponse> CheckStockAsync(int productId, int requiredQuantity)
    {
        try
        {
            _logger.LogInformation("Checking stock for product {ProductId}, quantity {Quantity} via gRPC",
                productId, requiredQuantity);

            var request = new CheckStockRequest
            {
                ProductId = productId,
                RequiredQuantity = requiredQuantity
            };
            var response = await _client.CheckStockAsync(request);

            _logger.LogInformation("Stock check result for product {ProductId}: {Available}",
                productId, response.Available);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock for product {ProductId} via gRPC", productId);
            throw;
        }
    }

    public async Task<UpdateStockResponse> UpdateStockAsync(int productId, int quantityChange)
    {
        try
        {
            _logger.LogInformation("Updating stock for product {ProductId}, change {Change} via gRPC",
                productId, quantityChange);

            var request = new UpdateStockRequest
            {
                ProductId = productId,
                QuantityChange = quantityChange
            };
            var response = await _client.UpdateStockAsync(request);

            _logger.LogInformation("Stock update result for product {ProductId}: {Success}, new stock: {NewStock}",
                productId, response.Success, response.NewStock);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product {ProductId} via gRPC", productId);
            throw;
        }
    }
}