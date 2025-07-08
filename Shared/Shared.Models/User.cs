using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

public class User
{
	public int Id { get; set; }

	[Required]
	[StringLength(100)]
	public string Username { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	public string PasswordHash { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string FullName { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public bool IsActive { get; set; } = true;
}