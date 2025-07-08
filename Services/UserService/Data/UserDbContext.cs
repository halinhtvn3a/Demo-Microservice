using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace UserService.Data;

public class UserDbContext : DbContext
{
	public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
	{
	}

	public DbSet<User> Users { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<User>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasIndex(e => e.Username).IsUnique();
			entity.HasIndex(e => e.Email).IsUnique();

			entity.Property(e => e.Username)
				.IsRequired()
				.HasMaxLength(100);

			entity.Property(e => e.Email)
				.IsRequired()
				.HasMaxLength(200);

			entity.Property(e => e.PasswordHash)
				.IsRequired();

			entity.Property(e => e.FullName)
				.IsRequired()
				.HasMaxLength(100);
		});

		// Seed data
		modelBuilder.Entity<User>().HasData(
			new User
			{
				Id = 1,
				Username = "admin",
				Email = "admin@demo.com",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
				FullName = "Administrator",
				CreatedAt = DateTime.UtcNow,
				IsActive = true
			},
			new User
			{
				Id = 2,
				Username = "user1",
				Email = "user1@demo.com",
				PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
				FullName = "Demo User 1",
				CreatedAt = DateTime.UtcNow,
				IsActive = true
			}
		);
	}
}