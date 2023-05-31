using Confluent.Kafka;

namespace Notification.Api.Kafka.Configurations;

public sealed record ProducerConfiguration(string Topic, ProducerConfig Config);
