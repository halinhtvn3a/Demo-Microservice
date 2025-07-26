using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.OrderNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.ShippingAddress)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.TotalAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>();

            // One-to-many relationship with OrderItems
            entity.HasMany(o => o.Items)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId);

            entity.Property(e => e.UnitPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            // TotalPrice is a computed property, not stored in database
            entity.Ignore(e => e.TotalPrice);

            // No foreign key to Product - microservices architecture
            // ProductId is just a reference, validation happens via API calls
        });

        // No User entity configuration - microservices architecture
        // UserId in Order is just a reference, validation happens via API calls
    }
}