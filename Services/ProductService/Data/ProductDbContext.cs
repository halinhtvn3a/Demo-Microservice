using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
	public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
	{
	}

	public DbSet<Product> Products { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Product>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasIndex(e => e.Name);
			entity.HasIndex(e => e.Category);

			entity.Property(e => e.Name)
				.IsRequired()
				.HasMaxLength(200);

			entity.Property(e => e.Description)
				.HasMaxLength(1000);

			entity.Property(e => e.Price)
				.IsRequired()
				.HasPrecision(18, 2);

			entity.Property(e => e.Category)
				.IsRequired()
				.HasMaxLength(100);

			entity.Property(e => e.ImageUrl)
				.HasMaxLength(500);
		});

		// Seed data
		modelBuilder.Entity<Product>().HasData(
			new Product
			{
				Id = 1,
				Name = "iPhone 15 Pro",
				Description = "Latest iPhone with advanced features",
				Price = 1199.99m,
				Stock = 50,
				Category = "Electronics",
				ImageUrl = "https://example.com/iphone15pro.jpg",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				IsActive = true
			},
			new Product
			{
				Id = 2,
				Name = "Samsung Galaxy S24",
				Description = "Premium Android smartphone",
				Price = 999.99m,
				Stock = 30,
				Category = "Electronics",
				ImageUrl = "https://example.com/galaxys24.jpg",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				IsActive = true
			},
			new Product
			{
				Id = 3,
				Name = "MacBook Pro M3",
				Description = "High-performance laptop for professionals",
				Price = 1999.99m,
				Stock = 20,
				Category = "Computers",
				ImageUrl = "https://example.com/macbookpro.jpg",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				IsActive = true
			},
			new Product
			{
				Id = 4,
				Name = "Nike Air Max",
				Description = "Comfortable running shoes",
				Price = 129.99m,
				Stock = 100,
				Category = "Shoes",
				ImageUrl = "https://example.com/nikeairmax.jpg",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				IsActive = true
			},
			new Product
			{
				Id = 5,
				Name = "Coffee Maker Pro",
				Description = "Professional grade coffee machine",
				Price = 299.99m,
				Stock = 15,
				Category = "Home & Kitchen",
				ImageUrl = "https://example.com/coffeemaker.jpg",
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				IsActive = true
			}
		);
	}
}