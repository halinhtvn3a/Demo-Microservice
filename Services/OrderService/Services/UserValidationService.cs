using OrderService.Clients;
using System.Net.Http;

namespace OrderService.Services;

public class UserValidationService : IUserValidationService
{
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<UserValidationService> _logger;

    public UserValidationService(
        IUserServiceClient userServiceClient,
        ILogger<UserValidationService> logger)
    {
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    public async Task<bool> ValidateUserExistsAsync(int userId)
    {
        try
        {
            // Validate user exists by calling UserService API
            var userDto = await _userServiceClient.GetUserAsync(userId, "");
            if (userDto != null)
            {
                _logger.LogInformation("User {UserId} validated successfully", userId);
                return true;
            }

            _logger.LogWarning("User {UserId} not found in UserService", userId);
            return false;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("refused"))
        {
            _logger.LogWarning(ex, "UserService is not available for user {UserId} validation. Allowing order creation for resilience.", userId);
            // In microservices, we allow degraded functionality when dependent services are down
            // This prevents cascading failures - the order can still be created
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating user {UserId}", userId);
            // For other errors, we're more cautious
            return false;
        }
    }
}