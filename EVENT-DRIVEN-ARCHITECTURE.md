# Event-Driven Architecture

Hệ thống microservices này sử dụng event-driven architecture với RabbitMQ để đảm bảo loose coupling và scalability.

## Tổng quan

Event-driven architecture cho phép các services giao tiếp thông qua events thay vì direct API calls, giúp:
- Giảm coupling giữa các services
- Tăng resilience và fault tolerance
- Dễ dàng scale và maintain
- Hỗ trợ eventual consistency

## Messaging Infrastructure

### RabbitMQ Setup
- **Exchange**: `microservice.events` (topic exchange)
- **Connection**: `amqp://guest:guest@localhost:5672`
- **Management UI**: http://localhost:15672

### Message Publisher
```csharp
public interface IMessagePublisher
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default);
}
```

### Message Consumer
```csharp
public interface IMessageConsumer
{
    void Subscribe<T>(string queueName, string routingKey, Func<T, Task> handler);
}
```

## Events và Handlers

### 1. Product Events

#### ProductStockUpdatedEvent
- **Publisher**: ProductService
- **Routing Key**: `product.stock-updated`
- **Consumers**: NotificationService (logging)
- **Trigger**: Khi stock của product thay đổi

```csharp
public record ProductStockUpdatedEvent(
    int ProductId,
    string ProductName,
    int OldStock,
    int NewStock,
    DateTime UpdatedAt
);
```

### 2. Order Events

#### OrderCreatedEvent
- **Publisher**: OrderService
- **Routing Key**: `order.created`
- **Consumers**: NotificationService (send confirmation email)
- **Trigger**: Khi order mới được tạo

```csharp
public record OrderCreatedEvent(
    int OrderId,
    int UserId,
    decimal TotalAmount,
    List<OrderItemEvent> Items,
    DateTime CreatedAt
);
```

#### OrderStatusChangedEvent
- **Publisher**: OrderService
- **Routing Key**: `order.status-changed`
- **Consumers**: NotificationService (send status update)
- **Trigger**: Khi status của order thay đổi

```csharp
public record OrderStatusChangedEvent(
    int OrderId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt
);
```

#### OrderCancelledEvent
- **Publisher**: OrderService
- **Routing Key**: `order.cancelled`
- **Consumers**: NotificationService (send cancellation notice)
- **Trigger**: Khi order bị cancel

```csharp
public record OrderCancelledEvent(
    int OrderId,
    int UserId,
    string Reason,
    DateTime CancelledAt
);
```

### 3. User Events

#### UserRegisteredEvent
- **Publisher**: UserService
- **Routing Key**: `user.registered`
- **Consumers**: NotificationService (send welcome email)
- **Trigger**: Khi user mới đăng ký

```csharp
public record UserRegisteredEvent(
    int UserId,
    string Email,
    string FullName,
    DateTime RegisteredAt
);
```

## Service Implementation

### Publishing Events

```csharp
// In ProductService
await _messagePublisher.PublishAsync("product.stock-updated", stockUpdatedEvent);

// In OrderService
await _messagePublisher.PublishAsync("order.created", orderCreatedEvent);

// In UserService
await _messagePublisher.PublishAsync("user.registered", userRegisteredEvent);
```

### Consuming Events

```csharp
// In NotificationService
public class OrderEventHandlers : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _messageConsumer.Subscribe<OrderCreatedEvent>("order.created", "order.created", HandleOrderCreated);
        _messageConsumer.Subscribe<OrderCompletedEvent>("order.completed", "order.completed", HandleOrderCompleted);
        _messageConsumer.Subscribe<UserRegisteredEvent>("user.registered", "user.registered", HandleUserRegistered);

        await _messageConsumer.StartAsync(stoppingToken);
    }
}
```

## Configuration

### Service Registration
```csharp
// In Program.cs của mỗi service
builder.Services.AddRabbitMQMessaging();
```

### Connection Strings
```json
{
  "ConnectionStrings": {
    "rabbitmq": "amqp://guest:guest@localhost:5672",
    "redis": "localhost:6379"
  }
}
```

## Testing

### 1. Start Services
```powershell
.\start-with-events.ps1
```

### 2. Check RabbitMQ Status
```powershell
.\check-rabbitmq.ps1
```

### 3. Test Events
```powershell
.\test-events.ps1
```

## Monitoring

### RabbitMQ Management UI
- URL: http://localhost:15672
- Username: guest
- Password: guest

### Key Metrics to Monitor
- Queue lengths
- Message rates
- Consumer counts
- Failed messages

### Queues to Watch
- `order.created`
- `order.status-changed`
- `order.cancelled`
- `user.registered`
- `product.stock-updated`

## Error Handling

### Message Retry
- Failed messages are nacked and can be requeued
- Dead letter queues can be configured for persistent failures

### Circuit Breaker
- Services should implement circuit breaker pattern for external dependencies
- Graceful degradation when messaging is unavailable

## Best Practices

### Event Design
1. **Immutable Events**: Events should be immutable records
2. **Versioning**: Plan for event schema evolution
3. **Idempotency**: Handlers should be idempotent
4. **Ordering**: Don't rely on message ordering unless guaranteed

### Performance
1. **Batching**: Consider batching for high-volume events
2. **Async Processing**: All event handlers should be async
3. **Resource Management**: Properly dispose connections and channels

### Reliability
1. **Durability**: Use durable exchanges and queues
2. **Acknowledgments**: Properly ack/nack messages
3. **Monitoring**: Monitor queue depths and processing times
4. **Alerting**: Set up alerts for failed messages

## Future Enhancements

### Planned Features
1. **Event Sourcing**: Store events as source of truth
2. **CQRS**: Separate read/write models
3. **Saga Pattern**: Distributed transaction management
4. **Event Replay**: Ability to replay events for recovery

### Scalability
1. **Partitioning**: Partition events by tenant or entity
2. **Clustering**: RabbitMQ clustering for HA
3. **Load Balancing**: Multiple consumer instances
4. **Caching**: Cache frequently accessed data

## Troubleshooting

### Common Issues
1. **Connection Failures**: Check RabbitMQ container status
2. **Queue Not Created**: Ensure consumers are running
3. **Messages Not Processed**: Check handler exceptions
4. **Performance Issues**: Monitor queue depths

### Debug Commands
```powershell
# Check RabbitMQ container
docker ps --filter "name=rabbitmq-microservices"

# View RabbitMQ logs
docker logs rabbitmq-microservices

# Check queue status
curl -u guest:guest http://localhost:15672/api/queues
```