namespace OrderService.Services;

public interface IUserValidationService
{
    Task<bool> ValidateUserExistsAsync(int userId);
}