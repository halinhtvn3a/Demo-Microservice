using OrderService.Clients;
using Shared.Models.DTOs;

namespace OrderService.Services;

public class MockUserServiceClient : IUserServiceClient
{
    private readonly ILogger<MockUserServiceClient> _logger;

    public MockUserServiceClient(ILogger<MockUserServiceClient> logger)
    {
        _logger = logger;
    }

    public Task<UserDto> GetUserAsync(int id, string token)
    {
        _logger.LogInformation("Mock get user {UserId}", id);
        return Task.FromResult(new UserDto
        {
            Id = id,
            Username = $"user{id}",
            Email = $"user{id}@example.com",
            FullName = $"User {id}"
        });
    }

    public Task<object> GetHealthAsync()
    {
        return Task.FromResult<object>(new { status = "healthy", service = "MockUserService" });
    }
}