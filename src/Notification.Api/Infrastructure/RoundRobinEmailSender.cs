using Notification.Api.Infrastructure.Provider;

namespace Notification.Api.Infrastructure;

internal sealed partial class RoundRobinEmailSender : IEmailSender
{
    private readonly ILogger<RoundRobinEmailSender> _logger;
    private readonly ProviderEntry[] _providers;
    private int _index;

    public RoundRobinEmailSender(IEnumerable<IEmailProvider> providers, ILogger<RoundRobinEmailSender> logger)
    {
        _logger = logger;
        _providers = providers
            .OrderBy(x => x.GetType().Name)
            .Select(x => new ProviderEntry(x))
            .ToArray();
    }

    public async Task SendAsync(EmailRequest message, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var count = _providers.Length;
        var startIndex = NextIndex(count);

        for (var i = 0; i < count; i++)
        {
            var provider = _providers[(startIndex + i) % count];

            if (!provider.IsAvailable(now))
            {
                LogProviderCooldown(provider.Name);
                continue;
            }

            try
            {
                await provider.SendAsync(message, ct);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (ProviderRateLimitException ex)
            {
                provider.Cooldown(now.Add(ex.RetryAfter));
                LogProviderRateLimited(provider.Name, provider.AvailableAfter, ex.RetryAfter);
            }
            catch (Exception ex)
            {
                LogProviderFailed(provider.Name, ex.Message);
            }
        }

        throw new InvalidOperationException("All email providers are exhausted");
    }

    private int NextIndex(int providers)
    {
        int current, next;

        do
        {
            current = _index;
            next = (current + 1) % providers;
        }
        while (Interlocked.CompareExchange(ref _index, next, current) != current);

        return next;
    }

    [LoggerMessage(LogLevel.Warning, "Provider {provider} rate limited, cooling down until {until} (retry after {retryAfter})")]
    partial void LogProviderRateLimited(string provider, DateTime until, TimeSpan retryAfter);

    [LoggerMessage(LogLevel.Debug, "Provider {provider} is in cooldown, skipping")]
    partial void LogProviderCooldown(string provider);

    [LoggerMessage(LogLevel.Warning, "Provider {provider} failed: {error}, trying next")]
    partial void LogProviderFailed(string provider, string error);

    private sealed class ProviderEntry
    {
        private readonly IEmailProvider _provider;

        public ProviderEntry(IEmailProvider provider)
        {
            _provider = provider;
            Name = provider.GetType().Name;
        }

        public string Name { get; }

        public DateTime AvailableAfter { get; private set; }

        public Task SendAsync(EmailRequest message, CancellationToken ct) =>
            _provider.SendAsync(message, ct);

        public bool IsAvailable(DateTime now) =>
            now >= AvailableAfter;

        public void Cooldown(DateTime delay) =>
            AvailableAfter = delay;
    }
}
