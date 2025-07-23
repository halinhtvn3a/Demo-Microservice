using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Text.RegularExpressions;

namespace NotificationService.Services;

public class EmailService : IEmailService
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<EmailService> _logger;

	public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
	{
		_configuration = configuration;
		_logger = logger;
	}

	public async Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? plainTextBody = null)
	{
		try
		{
			if (!IsValidEmail(to))
			{
				_logger.LogWarning("Invalid email address: {Email}", to);
				return false;
			}

			var message = new MimeMessage();
			message.From.Add(new MailboxAddress(
				_configuration["Email:FromName"] ?? "Demo Microservices",
				_configuration["Email:FromAddress"] ?? "noreply@demo.com"));

			message.To.Add(new MailboxAddress("", to));
			message.Subject = subject;

			var builder = new BodyBuilder();
			builder.HtmlBody = htmlBody;

			if (!string.IsNullOrEmpty(plainTextBody))
			{
				builder.TextBody = plainTextBody;
			}

			message.Body = builder.ToMessageBody();

			// In production, would configure SMTP settings
			_logger.LogInformation("EMAIL SENT:\nTo: {To}\nSubject: {Subject}\nBody: {Body}",
				to, subject, htmlBody);

			await Task.Delay(100);

			var random = new Random();
			if (random.Next(1, 101) <= 5)
			{
				_logger.LogWarning("Simulated email sending failure for: {Email}", to);
				return false;
			}

			_logger.LogInformation("Email sent successfully to: {Email}", to);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending email to: {Email}", to);
			return false;
		}
	}

	public async Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string htmlBody)
	{
		var tasks = recipients.Select(recipient => SendEmailAsync(recipient, subject, htmlBody));
		var results = await Task.WhenAll(tasks);

		var successCount = results.Count(r => r);
		_logger.LogInformation("Bulk email sent: {Success}/{Total} successful", successCount, recipients.Count);

		return successCount > 0;
	}

	public bool IsValidEmail(string email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return false;

		try
		{
			var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
			return emailRegex.IsMatch(email);
		}
		catch
		{
			return false;
		}
	}
}