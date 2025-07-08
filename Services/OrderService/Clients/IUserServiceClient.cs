using Refit;
using Shared.Models.DTOs;

namespace OrderService.Clients;

public interface IUserServiceClient
{
	[Get("/api/users/{id}")]
	Task<UserDto> GetUserAsync(int id, [Authorize("Bearer")] string token);

	[Get("/api/users/health")]
	Task<object> GetHealthAsync();
}