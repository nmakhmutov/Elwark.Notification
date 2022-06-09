using Notification.Api.Integration;

namespace Notification.Api.Kafka;

public interface IKafkaHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T message);
}
