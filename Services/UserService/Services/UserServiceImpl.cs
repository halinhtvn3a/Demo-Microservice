using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Shared.Models;
using Shared.Models.DTOs;
using UserService.Data;

namespace UserService.Services;

public class UserServiceImpl : IUserService
{
	private readonly UserDbContext _context;
	private readonly IConfiguration _configuration;
	private readonly HybridCache _cache;
	private readonly ILogger<UserServiceImpl> _logger;

	public UserServiceImpl(
		UserDbContext context,
		IConfiguration configuration,
		HybridCache cache,
		ILogger<UserServiceImpl> logger)
	{
		_context = context;
		_configuration = configuration;
		_cache = cache;
		_logger = logger;
	}

	public async Task<LoginResponse?> LoginAsync(LoginRequest request)
	{
		try
		{
			var user = await _context.Users
				.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

			if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
			{
				_logger.LogWarning("Invalid login attempt for username: {Username}", request.Username);
				return null;
			}

			var token = GenerateJwtToken(user);
			var expiresAt = DateTime.UtcNow.AddHours(24);

			// Cache user data
			await _cache.SetAsync($"user:{user.Id}", new UserDto
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				FullName = user.FullName
			}, TimeSpan.FromHours(1));

			_logger.LogInformation("User {Username} logged in successfully", user.Username);

			return new LoginResponse
			{
				Token = token,
				ExpiresAt = expiresAt,
				User = new UserDto
				{
					Id = user.Id,
					Username = user.Username,
					Email = user.Email,
					FullName = user.FullName
				}
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during login for username: {Username}", request.Username);
			return null;
		}
	}

	public async Task<UserDto?> RegisterAsync(RegisterRequest request)
	{
		try
		{
			// Check if user already exists
			if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
			{
				_logger.LogWarning("Registration failed: User with username {Username} or email {Email} already exists",
					request.Username, request.Email);
				return null;
			}

			var user = new User
			{
				Username = request.Username,
				Email = request.Email,
				PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
				FullName = request.FullName,
				CreatedAt = DateTime.UtcNow,
				IsActive = true
			};

			_context.Users.Add(user);
			await _context.SaveChangesAsync();

			_logger.LogInformation("User {Username} registered successfully", user.Username);

			return new UserDto
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				FullName = user.FullName
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
			return null;
		}
	}

	public async Task<UserDto?> GetUserByIdAsync(int id)
	{
		try
		{
			// Try cache first
			var cachedUser = await _cache.GetAsync<UserDto>($"user:{id}");
			if (cachedUser != null)
			{
				return cachedUser;
			}

			var user = await _context.Users
				.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

			if (user == null)
				return null;

			var userDto = new UserDto
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				FullName = user.FullName
			};

			// Cache for 1 hour
			await _cache.SetAsync($"user:{id}", userDto, TimeSpan.FromHours(1));

			return userDto;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting user by ID: {UserId}", id);
			return null;
		}
	}

	public async Task<UserDto?> GetUserByUsernameAsync(string username)
	{
		try
		{
			var user = await _context.Users
				.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

			if (user == null)
				return null;

			return new UserDto
			{
				Id = user.Id,
				Username = user.Username,
				Email = user.Email,
				FullName = user.FullName
			};
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error getting user by username: {Username}", username);
			return null;
		}
	}

	public Task<bool> ValidateTokenAsync(string token)
	{
		try
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "your-secret-key-here");

			tokenHandler.ValidateToken(token, new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(key),
				ValidateIssuer = false,
				ValidateAudience = false,
				ClockSkew = TimeSpan.Zero
			}, out SecurityToken validatedToken);

			return Task.FromResult(true);
		}
		catch
		{
			return Task.FromResult(false);
		}
	}

	public string GenerateJwtToken(User user)
	{
		var tokenHandler = new JwtSecurityTokenHandler();
		var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "your-secret-key-here");

		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(new[]
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
				new Claim(ClaimTypes.Name, user.Username),
				new Claim(ClaimTypes.Email, user.Email),
				new Claim("FullName", user.FullName)
			}),
			Expires = DateTime.UtcNow.AddHours(24),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
		};

		var token = tokenHandler.CreateToken(tokenDescriptor);
		return tokenHandler.WriteToken(token);
	}
}