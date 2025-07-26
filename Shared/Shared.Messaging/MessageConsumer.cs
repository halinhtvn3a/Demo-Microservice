using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Shared.Messaging;

public class MessageConsumer : BackgroundService, IMessageConsumer
{
    private readonly RabbitMQConnection _connection;
    private readonly ILogger<MessageConsumer> _logger;
    private readonly Dictionary<string, (Type messageType, Func<object, Task> handler)> _subscriptions = new();
    private IModel? _channel;

    public MessageConsumer(RabbitMQConnection connection, ILogger<MessageConsumer> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public void Subscribe<T>(string queueName, string routingKey, Func<T, Task> handler)
    {
        _subscriptions[queueName] = (typeof(T), async (obj) => await handler((T)obj));
        _logger.LogInformation("Subscribed to queue {QueueName} with routing key {RoutingKey}", queueName, routingKey);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateChannel();

        _channel.ExchangeDeclare("microservice.events", "topic", durable: true, autoDelete: false, arguments: null);

        foreach (var subscription in _subscriptions)
        {
            var queueName = subscription.Key;
            _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);

            // Bind queue to exchange with routing key pattern
            _channel.QueueBind(queueName, "microservice.events", queueName);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    var messageType = subscription.Value.messageType;
                    var deserializedMessage = JsonSerializer.Deserialize(message, messageType);

                    if (deserializedMessage != null)
                    {
                        await subscription.Value.handler(deserializedMessage);
                        _channel.BasicAck(ea.DeliveryTag, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(queueName, false, consumer);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Dispose();
        await base.StopAsync(cancellationToken);
    }
}