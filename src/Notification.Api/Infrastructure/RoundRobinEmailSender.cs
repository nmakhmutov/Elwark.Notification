using System.Collections.Concurrent;
using Notification.Api.Infrastructure.Provider;

namespace Notification.Api.Infrastructure;

internal sealed partial class RoundRobinEmailSender : IEmailSender
{
    private static readonly TimeSpan CooldownDuration = TimeSpan.FromMinutes(20);
    private readonly ConcurrentDictionary<Type, DateTime> _cooldowns = new();
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<RoundRobinEmailSender> _logger;
    private int _index;

    public RoundRobinEmailSender(IServiceScopeFactory factory, ILogger<RoundRobinEmailSender> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task SendAsync(EmailRequest message, CancellationToken ct)
    {
        await using var scope = _factory.CreateAsyncScope();

        var providers = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IEmailProvider>>()
            .Select(x => (Type: x.GetType(), Provider: x))
            .OrderBy(x => x.Type.Name)
            .ToArray();

        var now = DateTime.UtcNow;
        var count = providers.Length;
        var startIndex = NextIndex(count);

        for (var i = 0; i < count; i++)
        {
            var current = (startIndex + i) % count;
            var (type, provider) = providers[current];

            if (!IsAvailable(type, now))
            {
                LogProviderCooldown(type.Name);
                continue;
            }

            try
            {
                await provider.SendAsync(message, ct);
                return;
            }
            catch (ProviderRateLimitException ex)
            {
                var until = SetCooldown(type, now);
                LogProviderRateLimited(ex.Provider, until);
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

    private bool IsAvailable(Type type, DateTime now)
    {
        if (!_cooldowns.TryGetValue(type, out var until))
            return true;

        if (now < until)
            return false;

        _cooldowns.TryRemove(type, out _);
        return true;
    }

    private DateTime SetCooldown(Type type, DateTime now)
    {
        var until = now.Add(CooldownDuration);
        _cooldowns[type] = until;

        return until;
    }

    [LoggerMessage(LogLevel.Warning, "Provider {provider} rate limited, cooling down until {until}")]
    partial void LogProviderRateLimited(string provider, DateTime until);

    [LoggerMessage(LogLevel.Debug, "Provider {provider} is in cooldown, skipping")]
    partial void LogProviderCooldown(string provider);
}
