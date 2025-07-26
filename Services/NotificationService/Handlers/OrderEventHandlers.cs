using Shared.Events;
using Shared.Messaging;
using NotificationService.Services;

namespace NotificationService.Handlers;

public class OrderEventHandlers : BackgroundService
{
    private readonly IMessageConsumer _messageConsumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderEventHandlers> _logger;

    public OrderEventHandlers(
        IMessageConsumer messageConsumer,
        IServiceProvider serviceProvider,
        ILogger<OrderEventHandlers> logger)
    {
        _messageConsumer = messageConsumer;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to order events
        _messageConsumer.Subscribe<OrderCreatedEvent>("order.created", "order.created", HandleOrderCreated);
        _messageConsumer.Subscribe<OrderCompletedEvent>("order.completed", "order.completed", HandleOrderCompleted);
        _messageConsumer.Subscribe<UserRegisteredEvent>("user.registered", "user.registered", HandleUserRegistered);

        await _messageConsumer.StartAsync(stoppingToken);
    }

    private async Task HandleOrderCreated(OrderCreatedEvent orderEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        _logger.LogInformation("Processing order created event for Order {OrderId}", orderEvent.OrderId);

        // Send order confirmation email
        await emailService.SendEmailAsync(
            "customer@example.com", // In real app, get from user service
            "Order Confirmation",
            $"Your order #{orderEvent.OrderId} has been created successfully. Total: ${orderEvent.TotalAmount:F2}"
        );
    }

    private async Task HandleOrderCompleted(OrderCompletedEvent orderEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        _logger.LogInformation("Processing order completed event for Order {OrderId}", orderEvent.OrderId);

        // Send order completion email
        await emailService.SendEmailAsync(
            "customer@example.com", // In real app, get from user service
            "Order Completed",
            $"Your order #{orderEvent.OrderId} has been completed and shipped!"
        );
    }

    private async Task HandleUserRegistered(UserRegisteredEvent userEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        _logger.LogInformation("Processing user registered event for User {UserId}", userEvent.UserId);

        // Send welcome email
        await emailService.SendEmailAsync(
            userEvent.Email,
            "Welcome to MicroserviceDemo!",
            $"Hello {userEvent.FullName}, welcome to our platform!"
        );
    }
}