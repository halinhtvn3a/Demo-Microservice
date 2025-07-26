namespace OrderService.Services;

public class MockUserValidationService : IUserValidationService
{
    private readonly ILogger<MockUserValidationService> _logger;

    public MockUserValidationService(ILogger<MockUserValidationService> logger)
    {
        _logger = logger;
    }

    public Task<bool> ValidateUserExistsAsync(int userId)
    {
        _logger.LogInformation("Mock user validation for user {UserId} - always returns true", userId);
        return Task.FromResult(true);
    }
}