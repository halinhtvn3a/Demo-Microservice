using System.ComponentModel.DataAnnotations;

namespace Shared.Models.DTOs;

public class LoginRequest
{
	[Required]
	[StringLength(100)]
	public string Username { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
	public string Token { get; set; } = string.Empty;
	public DateTime ExpiresAt { get; set; }
	public UserDto User { get; set; } = new();
}

public class UserDto
{
	public int Id { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string FullName { get; set; } = string.Empty;
}

public class RegisterRequest
{
	[Required]
	[StringLength(100)]
	public string Username { get; set; } = string.Empty;

	[Required]
	[EmailAddress]
	public string Email { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string Password { get; set; } = string.Empty;

	[Required]
	[StringLength(100)]
	public string FullName { get; set; } = string.Empty;
}