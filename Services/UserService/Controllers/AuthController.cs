using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly ILogger<AuthController> _logger;

	public AuthController(IUserService userService, ILogger<AuthController> logger)
	{
		_userService = userService;
		_logger = logger;
	}

	/// <summary>
	/// Authenticate user and return JWT token
	/// </summary>
	/// <param name="request">Login credentials</param>
	/// <returns>JWT token and user information</returns>
	[HttpPost("login")]
	[ProducesResponseType(typeof(LoginResponse), 200)]
	[ProducesResponseType(400)]
	[ProducesResponseType(401)]
	public async Task<IActionResult> Login([FromBody] LoginRequest request)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var result = await _userService.LoginAsync(request);
		if (result == null)
		{
			return Unauthorized(new { message = "Invalid username or password" });
		}

		_logger.LogInformation("User {Username} logged in successfully", request.Username);
		return Ok(result);
	}

	/// <summary>
	/// Register a new user
	/// </summary>
	/// <param name="request">Registration details</param>
	/// <returns>Created user information</returns>
	[HttpPost("register")]
	[ProducesResponseType(typeof(UserDto), 201)]
	[ProducesResponseType(400)]
	[ProducesResponseType(409)]
	public async Task<IActionResult> Register([FromBody] RegisterRequest request)
	{
		if (!ModelState.IsValid)
		{
			return BadRequest(ModelState);
		}

		var result = await _userService.RegisterAsync(request);
		if (result == null)
		{
			return Conflict(new { message = "User with this username or email already exists" });
		}

		_logger.LogInformation("New user {Username} registered successfully", request.Username);
		return CreatedAtAction(nameof(GetProfile), new { id = result.Id }, result);
	}

	/// <summary>
	/// Get current user profile
	/// </summary>
	/// <returns>Current user information</returns>
	[HttpGet("profile")]
	[Authorize]
	[ProducesResponseType(typeof(UserDto), 200)]
	[ProducesResponseType(401)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> GetProfile()
	{
		var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
		if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
		{
			return Unauthorized();
		}

		var user = await _userService.GetUserByIdAsync(userId);
		if (user == null)
		{
			return NotFound();
		}

		return Ok(user);
	}

	/// <summary>
	/// Validate JWT token
	/// </summary>
	/// <param name="token">JWT token to validate</param>
	/// <returns>Token validation result</returns>
	[HttpPost("validate")]
	[ProducesResponseType(typeof(object), 200)]
	[ProducesResponseType(400)]
	public async Task<IActionResult> ValidateToken([FromBody] string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return BadRequest(new { message = "Token is required" });
		}

		var isValid = await _userService.ValidateTokenAsync(token);
		return Ok(new { isValid });
	}
}