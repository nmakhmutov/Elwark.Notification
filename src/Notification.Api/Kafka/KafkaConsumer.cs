using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using Notification.Api.Integration;
using Polly;
using Polly.Retry;

namespace Notification.Api.Kafka;

internal sealed class KafkaConsumer<Event, Handler> : BackgroundService
    where Event : IIntegrationEvent
    where Handler : class, IKafkaHandler<Event>
{
    private readonly IHostApplicationLifetime _application;
    private readonly ConsumerConfig _consumerConfig;
    private readonly KafkaConsumerConfig<Event> _handlerOptions;
    private readonly ILogger<KafkaConsumer<Event, Handler>> _logger;
    private readonly AsyncRetryPolicy _policy;
    private readonly IServiceScopeFactory _serviceFactory;

    public KafkaConsumer(IServiceScopeFactory serviceFactory, IHostApplicationLifetime application,
        ILogger<KafkaConsumer<Event, Handler>> logger, IOptions<ConsumerConfig> consumerOptions,
        IOptions<KafkaConsumerConfig<Event>> handlerOptions)
    {
        _logger = logger;
        _application = application;
        _serviceFactory = serviceFactory;
        _consumerConfig = consumerOptions.Value;
        _handlerOptions = handlerOptions.Value;

        _policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                _handlerOptions.RetryCount,
                _ => _handlerOptions.RetryInterval,
                (ex, time, retry, _) =>
                    _logger.LogCritical(ex, "Error occured in kafka handler for {N}. Retry {R} ({T})",
                        _handlerOptions.EventName, retry, time)
            );
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        switch (_handlerOptions.Threads)
        {
            case 0:
                _logger.LogWarning("Topic ({T}) doesn't have any consumer handlers for messages ({M})",
                    _handlerOptions.Topic, _handlerOptions.EventName);
                break;

            case 1:
                await CreateTopicIfNotExistsAsync();
                await Task.Factory.StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning);
                break;

            default:
                await CreateTopicIfNotExistsAsync();

                var tasks = new Task[_handlerOptions.Threads];
                for (var i = 0; i < _handlerOptions.Threads; i++)
                    tasks[i] = Task.Factory.StartNew(() => CreateConsumer(ct), TaskCreationOptions.LongRunning);

                await Task.WhenAll(tasks);
                break;
        }
    }

    private async Task CreateConsumer(CancellationToken ct)
    {
        var builder = new ConsumerBuilder<string, Event>(_consumerConfig)
            .SetValueDeserializer(KafkaDataConverter<Event>.Instance);

        using var consumer = builder.Build();
        consumer.Subscribe(_handlerOptions.Topic);

        _logger.LogInformation("Topic {T}. Consumer {C}. Message {M}. Subscribed.",
            _handlerOptions.Topic, consumer.MemberId, _handlerOptions.EventName);

        ct.Register(() => _logger.LogInformation("Topic {T}. Consumer {C}. Message {M}. Stopping...",
            _handlerOptions.Topic, consumer.MemberId, _handlerOptions.EventName));

        while (!ct.IsCancellationRequested)
            await ConsumeAsync(consumer, ct);
    }

    private async Task ConsumeAsync(IConsumer<string, Event> consumer, CancellationToken ct)
    {
        try
        {
            var result = consumer.Consume(ct);
            if (result.IsPartitionEOF)
                return;

            _logger.LogInformation("Topic {T}. Consumer {C}. Message {M}. Handling...",
                _handlerOptions.Topic, consumer.MemberId, _handlerOptions.EventName);

            await using var scope = _serviceFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<Event>>();

            await _policy.ExecuteAsync(() => handler.HandleAsync(result.Message.Value));

            _logger.LogInformation("Topic {T}. Consumer {C}. Message {M}. Handled.",
                _handlerOptions.Topic, consumer.MemberId, _handlerOptions.EventName);

            consumer.Commit(result);
        }
        catch (OperationCanceledException ex)
        {
            consumer.Close();
            _logger.LogWarning(ex, "Topic {T}. Consumer {C}. Message {M}. Canceled.",
                _handlerOptions.Topic, consumer.MemberId, _handlerOptions.EventName);

            throw;
        }
        catch (ConsumeException ex) when (ex.Error.IsFatal)
        {
            _logger.LogCritical(ex, "Topic {T}. Consumer {C}. Message {M}. Consumer exception has occured. Stopping...",
                _handlerOptions.Topic, consumer.MemberId, _handlerOptions.EventName);
            _application.StopApplication();

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Topic {T}. Consumer {C}. Message {M}. Unhandled exception has occured.",
                _handlerOptions.Topic, consumer.MemberId, _handlerOptions.EventName);
        }
    }

    private async Task CreateTopicIfNotExistsAsync()
    {
        var config = new AdminClientConfig { BootstrapServers = _consumerConfig.BootstrapServers };
        var builder = new AdminClientBuilder(config);
        var topic = new TopicSpecification
        {
            Name = _handlerOptions.Topic,
            NumPartitions = _handlerOptions.Threads,
            ReplicationFactor = -1
        };

        using var client = builder.Build();

        try
        {
            await client.CreateTopicsAsync(new[] { topic });

            _logger.LogInformation("Topic {T} created with {P} partitions", topic.Name, topic.NumPartitions);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(x => x.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // everything is fine!
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Exception occured on kafka topic creation");
            _application.StopApplication();

            throw;
        }
    }
}
