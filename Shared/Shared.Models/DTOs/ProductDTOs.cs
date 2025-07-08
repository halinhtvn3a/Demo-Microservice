using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs;

public class ProductDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public decimal Price { get; set; }
	public int Stock { get; set; }
	public string Category { get; set; } = string.Empty;
	public string ImageUrl { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public bool IsActive { get; set; }
}

public class CreateProductRequest
{
	[Required]
	[StringLength(200)]
	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	[Required]
	[Range(0.01, double.MaxValue)]
	public decimal Price { get; set; }

	[Required]
	[Range(0, int.MaxValue)]
	public int Stock { get; set; }

	[Required]
	[StringLength(100)]
	public string Category { get; set; } = string.Empty;

	public string ImageUrl { get; set; } = string.Empty;
}

public class UpdateProductRequest
{
	[StringLength(200)]
	public string? Name { get; set; }

	public string? Description { get; set; }

	[Range(0.01, double.MaxValue)]
	public decimal? Price { get; set; }

	[Range(0, int.MaxValue)]
	public int? Stock { get; set; }

	[StringLength(100)]
	public string? Category { get; set; }

	public string? ImageUrl { get; set; }
}

public class ProductSearchRequest
{
	public string? Name { get; set; }
	public string? Category { get; set; }
	public decimal? MinPrice { get; set; }
	public decimal? MaxPrice { get; set; }
	public bool? InStock { get; set; }
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 10;
}