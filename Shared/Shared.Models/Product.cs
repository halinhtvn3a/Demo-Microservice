using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public class Product
{
	public int Id { get; set; }

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

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

	public bool IsActive { get; set; } = true;
}