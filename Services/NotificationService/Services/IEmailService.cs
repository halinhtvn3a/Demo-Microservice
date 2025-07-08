namespace NotificationService.Services;

public interface IEmailService
{
	Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? plainTextBody = null);
	Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody);
	bool IsValidEmail(string email);
}