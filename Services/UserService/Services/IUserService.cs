using Shared.Models;
using Shared.Models.DTOs;

namespace UserService.Services;

public interface IUserService
{
	Task<LoginResponse?> LoginAsync(LoginRequest request);
	Task<UserDto?> RegisterAsync(RegisterRequest request);
	Task<UserDto?> GetUserByIdAsync(int id);
	Task<UserDto?> GetUserByUsernameAsync(string username);
	Task<bool> ValidateTokenAsync(string token);
	string GenerateJwtToken(User user);
}