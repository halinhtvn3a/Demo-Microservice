using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.Messaging;

public class RabbitMQConnection : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMQConnection> _logger;
    private IConnection? _connection;
    private readonly object _lock = new();

    public RabbitMQConnection(IConfiguration configuration, ILogger<RabbitMQConnection> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public IModel CreateChannel()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            Connect();
        }

        return _connection!.CreateModel();
    }

    private void Connect()
    {
        lock (_lock)
        {
            if (_connection != null && _connection.IsOpen)
                return;

            var connectionString = _configuration.GetConnectionString("rabbitmq") ?? "amqp://guest:guest@localhost:5672";

            _logger.LogInformation("Connecting to RabbitMQ: {ConnectionString}", connectionString);

            var factory = new ConnectionFactory();
            factory.Uri = new Uri(connectionString);
            factory.DispatchConsumersAsync = true;

            _connection = factory.CreateConnection();

            _logger.LogInformation("Connected to RabbitMQ successfully");
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}