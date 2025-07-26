using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Shared.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default);
    Task PublishAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default);
}

public interface IMessageConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void Subscribe<T>(string queueName, string routingKey, Func<T, Task> handler);
}

public class MessagePublisher : IMessagePublisher
{
    private readonly RabbitMQConnection _connection;
    private readonly ILogger<MessagePublisher> _logger;

    public MessagePublisher(RabbitMQConnection connection, ILogger<MessagePublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using var channel = _connection.CreateChannel();

            channel.ExchangeDeclare(exchange, "topic", durable: true, autoDelete: false, arguments: null);

            var json = JsonSerializer.Serialize(message);
            var body = System.Text.Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange, routingKey, mandatory: false, basicProperties: null, body: body);

            _logger.LogInformation("Published message to exchange {Exchange} with routing key {RoutingKey}", exchange, routingKey);
        }, cancellationToken);
    }

    public Task PublishAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default)
    {
        return PublishAsync("microservice.events", routingKey, message, cancellationToken);
    }
}