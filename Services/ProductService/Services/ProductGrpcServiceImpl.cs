using AutoMapper;
using Grpc.Core;
using ProductService.Protos;
using ProductService.Services;

namespace ProductService.Services;

public class ProductGrpcServiceImpl : ProductGrpcService.ProductGrpcServiceBase
{
	private readonly IProductService _productService;
	private readonly IMapper _mapper;
	private readonly ILogger<ProductGrpcServiceImpl> _logger;

	public ProductGrpcServiceImpl(
		IProductService productService,
		IMapper mapper,
		ILogger<ProductGrpcServiceImpl> logger)
	{
		_productService = productService;
		_mapper = mapper;
		_logger = logger;
	}

	public override async Task<ProductResponse> GetProduct(GetProductRequest request, ServerCallContext context)
	{
		try
		{
			_logger.LogInformation("gRPC GetProduct called for ID: {ProductId}", request.Id);

			var product = await _productService.GetProductByIdAsync(request.Id);
			if (product == null)
			{
				throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID {request.Id} not found"));
			}

			var response = new ProductResponse
			{
				Id = product.Id,
				Name = product.Name,
				Description = product.Description,
				Price = (double)product.Price,
				Stock = product.Stock,
				Category = product.Category,
				ImageUrl = product.ImageUrl,
				CreatedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				IsActive = product.IsActive
			};

			return response;
		}
		catch (RpcException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in gRPC GetProduct for ID: {ProductId}", request.Id);
			throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
		}
	}

	public override async Task<GetProductsResponse> GetProducts(GetProductsRequest request, ServerCallContext context)
	{
		try
		{
			_logger.LogInformation("gRPC GetProducts called");

			var searchRequest = new Shared.Models.DTOs.ProductSearchRequest
			{
				Name = string.IsNullOrEmpty(request.Name) ? null : request.Name,
				Category = string.IsNullOrEmpty(request.Category) ? null : request.Category,
				MinPrice = request.MinPrice > 0 ? request.MinPrice : null,
				MaxPrice = request.MaxPrice > 0 ? request.MaxPrice : null,
				InStock = request.InStock ? true : null,
				Page = request.Page > 0 ? request.Page : 1,
				PageSize = request.PageSize > 0 ? request.PageSize : 10
			};

			var products = await _productService.GetProductsAsync(searchRequest);

			var response = new GetProductsResponse
			{
				TotalCount = products.Count(),
				Page = searchRequest.Page,
				PageSize = searchRequest.PageSize
			};

			foreach (var product in products)
			{
				response.Products.Add(new ProductResponse
				{
					Id = product.Id,
					Name = product.Name,
					Description = product.Description,
					Price = (double)product.Price,
					Stock = product.Stock,
					Category = product.Category,
					ImageUrl = product.ImageUrl,
					CreatedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
					UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
					IsActive = product.IsActive
				});
			}

			return response;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in gRPC GetProducts");
			throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
		}
	}

	public override async Task<ProductResponse> CreateProduct(Protos.CreateProductRequest request, ServerCallContext context)
	{
		try
		{
			_logger.LogInformation("gRPC CreateProduct called for: {ProductName}", request.Name);

			var createRequest = new Shared.Models.DTOs.CreateProductRequest
			{
				Name = request.Name,
				Description = request.Description,
				Price = (decimal)request.Price,
				Stock = request.Stock,
				Category = request.Category,
				ImageUrl = request.ImageUrl
			};

			var product = await _productService.CreateProductAsync(createRequest);
			if (product == null)
			{
				throw new RpcException(new Status(StatusCode.InvalidArgument, "Failed to create product"));
			}

			return new ProductResponse
			{
				Id = product.Id,
				Name = product.Name,
				Description = product.Description,
				Price = (double)product.Price,
				Stock = product.Stock,
				Category = product.Category,
				ImageUrl = product.ImageUrl,
				CreatedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				IsActive = product.IsActive
			};
		}
		catch (RpcException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in gRPC CreateProduct");
			throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
		}
	}

	public override async Task<ProductResponse> UpdateProduct(Protos.UpdateProductRequest request, ServerCallContext context)
	{
		try
		{
			_logger.LogInformation("gRPC UpdateProduct called for ID: {ProductId}", request.Id);

			var updateRequest = new Shared.Models.DTOs.UpdateProductRequest();

			if (!string.IsNullOrEmpty(request.Name))
				updateRequest.Name = request.Name;
			if (!string.IsNullOrEmpty(request.Description))
				updateRequest.Description = request.Description;
			if (request.Price > 0)
				updateRequest.Price = (decimal)request.Price;
			if (request.Stock >= 0)
				updateRequest.Stock = request.Stock;
			if (!string.IsNullOrEmpty(request.Category))
				updateRequest.Category = request.Category;
			if (!string.IsNullOrEmpty(request.ImageUrl))
				updateRequest.ImageUrl = request.ImageUrl;

			var product = await _productService.UpdateProductAsync(request.Id, updateRequest);
			if (product == null)
			{
				throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID {request.Id} not found"));
			}

			return new ProductResponse
			{
				Id = product.Id,
				Name = product.Name,
				Description = product.Description,
				Price = (double)product.Price,
				Stock = product.Stock,
				Category = product.Category,
				ImageUrl = product.ImageUrl,
				CreatedAt = product.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				UpdatedAt = product.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
				IsActive = product.IsActive
			};
		}
		catch (RpcException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in gRPC UpdateProduct for ID: {ProductId}", request.Id);
			throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
		}
	}

	public override async Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context)
	{
		try
		{
			_logger.LogInformation("gRPC DeleteProduct called for ID: {ProductId}", request.Id);

			var success = await _productService.DeleteProductAsync(request.Id);

			return new DeleteProductResponse
			{
				Success = success,
				Message = success ? "Product deleted successfully" : "Product not found"
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in gRPC DeleteProduct for ID: {ProductId}", request.Id);
			throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
		}
	}

	public override async Task<CheckStockResponse> CheckStock(CheckStockRequest request, ServerCallContext context)
	{
		try
		{
			_logger.LogInformation("gRPC CheckStock called for product {ProductId}, quantity {Quantity}",
				request.ProductId, request.RequiredQuantity);

			var available = await _productService.CheckStockAsync(request.ProductId, request.RequiredQuantity);
			var currentStock = await _productService.GetCurrentStockAsync(request.ProductId);

			return new CheckStockResponse
			{
				Available = available,
				CurrentStock = currentStock,
				Message = available ? "Stock available" : "Insufficient stock"
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in gRPC CheckStock for product {ProductId}", request.ProductId);
			throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
		}
	}

	public override async Task<UpdateStockResponse> UpdateStock(UpdateStockRequest request, ServerCallContext context)
	{
		try
		{
			_logger.LogInformation("gRPC UpdateStock called for product {ProductId}, change {Change}",
				request.ProductId, request.QuantityChange);

			var success = await _productService.UpdateStockAsync(request.ProductId, request.QuantityChange);
			if (!success)
			{
				return new UpdateStockResponse
				{
					Success = false,
					NewStock = await _productService.GetCurrentStockAsync(request.ProductId),
					Message = "Failed to update stock. Product not found or insufficient stock."
				};
			}

			var newStock = await _productService.GetCurrentStockAsync(request.ProductId);

			return new UpdateStockResponse
			{
				Success = true,
				NewStock = newStock,
				Message = "Stock updated successfully"
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in gRPC UpdateStock for product {ProductId}", request.ProductId);
			throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
		}
	}
}