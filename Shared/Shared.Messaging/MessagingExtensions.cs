using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shared.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMQMessaging(this IServiceCollection services)
    {
        services.AddSingleton<RabbitMQConnection>();
        services.AddSingleton<IMessagePublisher, MessagePublisher>();
        services.AddSingleton<IMessageConsumer, MessageConsumer>();
        services.AddHostedService<MessageConsumer>();

        return services;
    }
}