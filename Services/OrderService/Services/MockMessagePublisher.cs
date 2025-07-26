using Shared.Messaging;

namespace OrderService.Services;

public class MockMessagePublisher : IMessagePublisher
{
    private readonly ILogger<MockMessagePublisher> _logger;

    public MockMessagePublisher(ILogger<MockMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock publish to exchange {Exchange} with routing key {RoutingKey}: {@Message}", exchange, routingKey, message);
        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock publish to routing key {RoutingKey}: {@Message}", routingKey, message);
        return Task.CompletedTask;
    }
}