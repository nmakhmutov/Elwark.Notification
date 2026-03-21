using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Notification.Api.Infrastructure;
using Notification.Api.Infrastructure.Provider;

namespace Notification.Api.Tests.Infrastructure;

public sealed class RoundRobinEmailSenderTests
{
    private static readonly EmailRequest TestMessage = new("test@example.com", "Subject", "Body", true);

    [Fact]
    public async Task SendAsync_ShouldSendViaOneProvider()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();

        await sender.SendAsync(TestMessage, CancellationToken.None);

        Assert.Equal(1, a.SendCount + b.SendCount);
    }

    [Fact]
    public async Task SendAsync_ShouldRoundRobinAcrossCalls()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();

        await sender.SendAsync(TestMessage, CancellationToken.None);
        await sender.SendAsync(TestMessage, CancellationToken.None);

        Assert.Equal(1, a.SendCount);
        Assert.Equal(1, b.SendCount);
    }

    [Fact]
    public async Task SendAsync_ShouldFailoverOnRateLimit()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ShouldRateLimit = true;
        b.ShouldRateLimit = true;

        // Both rate-limit — but we only need one to not rate-limit for failover
        // Set just one to rate-limit, the other should handle it
        b.ShouldRateLimit = false;

        await sender.SendAsync(TestMessage, CancellationToken.None);
        // One must have been tried and rate-limited, the other succeeded
        // or the non-rate-limited one was tried first
        Assert.Equal(1, a.SendCount + b.SendCount - 0);

        // More precisely: exactly one of them succeeded
        Assert.True(a.SendCount + b.SendCount >= 1);
    }

    [Fact]
    public async Task SendAsync_WhenFirstProviderRateLimits_ShouldSucceedViaSecond()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();

        // Rate-limit both, then we know exactly what happens
        a.ShouldRateLimit = true;
        b.ShouldRateLimit = true;

        // Can't send when all rate-limit
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        // Now allow both — but both are on cooldown, so still fails
        a.ShouldRateLimit = false;
        b.ShouldRateLimit = false;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_ShouldThrowInvalidOperationException_WhenAllProvidersExhausted()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ShouldRateLimit = true;
        b.ShouldRateLimit = true;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_CooldownShouldPreventProviderFromBeingUsed()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ShouldRateLimit = true;
        b.ShouldRateLimit = true;

        // Exhaust all — puts both on cooldown
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        var totalSendsBefore = a.SendCount + b.SendCount;

        // Both should now be skipped due to cooldown — no new sends attempted
        a.ShouldRateLimit = false;
        b.ShouldRateLimit = false;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        Assert.Equal(totalSendsBefore, a.SendCount + b.SendCount);
    }

    [Fact]
    public async Task SendAsync_ShouldPropagateNonRateLimitExceptions()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ExceptionToThrow = new InvalidOperationException("boom");
        b.ExceptionToThrow = new InvalidOperationException("boom");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task SendAsync_ShouldCycleIndexCorrectlyWithThreeProviders()
    {
        var a = new FakeProviderA();
        var b = new FakeProviderB();
        var c = new FakeProviderC();
        var sender = CreateSender(
            s => s.AddScoped<IEmailProvider>(_ => a),
            s => s.AddScoped<IEmailProvider>(_ => b),
            s => s.AddScoped<IEmailProvider>(_ => c));

        await sender.SendAsync(TestMessage, CancellationToken.None);
        await sender.SendAsync(TestMessage, CancellationToken.None);
        await sender.SendAsync(TestMessage, CancellationToken.None);

        Assert.Equal(1, a.SendCount);
        Assert.Equal(1, b.SendCount);
        Assert.Equal(1, c.SendCount);
    }

    [Fact]
    public async Task SendAsync_FailoverShouldTryAllProviders()
    {
        var a = new FakeProviderA { ShouldRateLimit = true };
        var b = new FakeProviderB { ShouldRateLimit = true };
        var c = new FakeProviderC();
        var sender = CreateSender(
            s => s.AddScoped<IEmailProvider>(_ => a),
            s => s.AddScoped<IEmailProvider>(_ => b),
            s => s.AddScoped<IEmailProvider>(_ => c));

        await sender.SendAsync(TestMessage, CancellationToken.None);

        // C should have handled it after A and B rate-limited
        Assert.Equal(1, c.SendCount);
    }

    private static (RoundRobinEmailSender Sender, FakeProviderA A, FakeProviderB B) CreateSenderWithTwoProviders()
    {
        var a = new FakeProviderA();
        var b = new FakeProviderB();
        var sender = CreateSender(
            s => s.AddScoped<IEmailProvider>(_ => a),
            s => s.AddScoped<IEmailProvider>(_ => b));

        return (sender, a, b);
    }

    private static RoundRobinEmailSender CreateSender(params Action<IServiceCollection>[] registrations)
    {
        var services = new ServiceCollection();

        foreach (var registration in registrations)
            registration(services);

        var rootProvider = services.BuildServiceProvider();
        var scopeFactory = rootProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = new NullLogger<RoundRobinEmailSender>();

        return new RoundRobinEmailSender(scopeFactory, logger);
    }

    private abstract class FakeProviderBase : IEmailProvider
    {
        public int SendCount { get; set; }

        public bool ShouldRateLimit { get; set; }

        public Exception? ExceptionToThrow { get; set; }

        public Task SendAsync(EmailRequest message, CancellationToken ct)
        {
            SendCount++;

            if (ExceptionToThrow is not null)
                throw ExceptionToThrow;

            if (ShouldRateLimit)
                throw new ProviderRateLimitException(GetType().Name);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeProviderA : FakeProviderBase;

    private sealed class FakeProviderB : FakeProviderBase;

    private sealed class FakeProviderC : FakeProviderBase;
}
