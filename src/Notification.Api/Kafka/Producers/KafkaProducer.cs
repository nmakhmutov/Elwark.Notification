using Confluent.Kafka;
using Notification.Api.Integration;
using Notification.Api.Kafka.Configurations;
using Notification.Api.Kafka.Converters;

namespace Notification.Api.Kafka.Producers;

internal sealed class KafkaProducer<T> : IKafkaProducer<T> where T : IIntegrationEvent
{
    private readonly IProducer<Guid, T> _producer;
    private readonly string _topic;

    public KafkaProducer(ProducerConfiguration configuration, ILogger logger)
    {
        _topic = configuration.Topic;

        _producer = new ProducerBuilder<Guid, T>(configuration.Config)
            .SetErrorHandler((_, error) => logger.LogError("Error occured on publishing {E}", error))
            .SetKeySerializer(KafkaKeyConverter.Instance)
            .SetValueSerializer(KafkaValueConverter<T>.Instance)
            .Build();
    }

    public Task ProduceAsync(object message, CancellationToken ct) =>
        ProduceAsync((T)message, ct);

    public Task ProduceAsync(T message, CancellationToken ct)
    {
        var kafkaMessage = new Message<Guid, T>
        {
            Key = message.MessageId,
            Value = message,
            Timestamp = new Timestamp(message.CreatedAt)
        };

        return _producer.ProduceAsync(_topic, kafkaMessage, ct);
    }

    public void Dispose() =>
        _producer.Dispose();
}
