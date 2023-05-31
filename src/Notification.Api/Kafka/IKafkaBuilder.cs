namespace Notification.Api.Kafka;

public interface IKafkaBuilder
{
    public IServiceCollection Services { get; }

    internal string Brokers { get; }
}
