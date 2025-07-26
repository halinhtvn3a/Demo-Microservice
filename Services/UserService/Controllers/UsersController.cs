using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.DTOs;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
	private readonly IUserService _userService;
	private readonly ILogger<UsersController> _logger;

	public UsersController(IUserService userService, ILogger<UsersController> logger)
	{
		_userService = userService;
		_logger = logger;
	}

	/// <summary>
	/// Get user by ID
	/// </summary>
	/// <param name="id">User ID</param>
	/// <returns>User information</returns>
	[HttpGet("{id}")]
	[ProducesResponseType(typeof(UserDto), 200)]
	[ProducesResponseType(401)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> GetUser(int id)
	{
		var user = await _userService.GetUserByIdAsync(id);
		if (user == null)
		{
			return NotFound();
		}

		return Ok(user);
	}

	/// <summary>
	/// Get user by username
	/// </summary>
	/// <param name="username">Username</param>
	/// <returns>User information</returns>
	[HttpGet("username/{username}")]
	[ProducesResponseType(typeof(UserDto), 200)]
	[ProducesResponseType(401)]
	[ProducesResponseType(404)]
	public async Task<IActionResult> GetUserByUsername(string username)
	{
		var user = await _userService.GetUserByUsernameAsync(username);
		if (user == null)
		{
			return NotFound();
		}

		return Ok(user);
	}

	/// <summary>
	/// Health check endpoint
	/// </summary>
	/// <returns>Health status</returns>
	[HttpGet("health")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(object), 200)]
	public IActionResult Health()
	{
		return Ok(new
		{
			status = "healthy",
			service = "UserService",
			timestamp = DateTime.UtcNow
		});
	}
}