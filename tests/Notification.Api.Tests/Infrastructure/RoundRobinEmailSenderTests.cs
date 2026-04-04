using Microsoft.Extensions.Logging.Abstractions;
using Notification.Api.Infrastructure;
using Notification.Api.Infrastructure.Provider;

namespace Notification.Api.Tests.Infrastructure;

public sealed class RoundRobinEmailSenderTests
{
    private static readonly EmailRequest TestMessage = new("test@example.com", "Subject", "Body", true);
    private static readonly TimeSpan TestCooldown = TimeSpan.FromMinutes(10);

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
    public async Task SendAsync_WhenProviderRateLimits_ShouldSucceedViaOther()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ShouldRateLimit = true;
        b.ShouldRateLimit = true;

        // Whichever is tried first will rate-limit, turn off rate-limiting for the other
        // Since we don't know the order, set both to rate-limit then clear one
        a.ShouldRateLimit = false;

        await sender.SendAsync(TestMessage, CancellationToken.None);

        Assert.True(a.SendCount + b.SendCount >= 1);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowWhenAllProvidersRateLimited()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ShouldRateLimit = true;
        b.ShouldRateLimit = true;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        Assert.Equal("All email providers are exhausted", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public async Task SendAsync_CooldownShouldPreventProviderFromBeingUsed()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ShouldRateLimit = true;
        b.ShouldRateLimit = true;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        var totalSendsBefore = a.SendCount + b.SendCount;

        a.ShouldRateLimit = false;
        b.ShouldRateLimit = false;

        // Both on cooldown, so they should be skipped even though they'd succeed now
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        Assert.Equal(totalSendsBefore, a.SendCount + b.SendCount);
    }

    [Fact]
    public async Task SendAsync_WhenProviderFails_ShouldTryNextProvider()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ExceptionToThrow = new InvalidOperationException("boom");
        b.ExceptionToThrow = new InvalidOperationException("boom");

        // Whichever is tried first fails, the second also fails
        // Clear one so the failover target succeeds
        b.ExceptionToThrow = null;

        await sender.SendAsync(TestMessage, CancellationToken.None);

        // At least one provider was tried and the other succeeded
        Assert.True(a.SendCount + b.SendCount >= 1);
    }

    [Fact]
    public async Task SendAsync_WhenProviderFails_ShouldNotCooldown()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ExceptionToThrow = new InvalidOperationException("boom");
        b.ExceptionToThrow = new InvalidOperationException("boom");

        // Both fail → exception thrown
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        // Clear exceptions — providers should NOT be on cooldown
        a.ExceptionToThrow = null;
        b.ExceptionToThrow = null;

        // Should succeed because non-rate-limit failures don't cause cooldown
        await sender.SendAsync(TestMessage, CancellationToken.None);

        Assert.True(a.SendCount + b.SendCount >= 3);
    }

    [Fact]
    public async Task SendAsync_WhenAllProvidersFail_ShouldThrow()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ExceptionToThrow = new InvalidOperationException("boom a");
        b.ExceptionToThrow = new InvalidOperationException("boom b");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        Assert.Equal("All email providers are exhausted", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public async Task SendAsync_ShouldCycleIndexCorrectlyWithThreeProviders()
    {
        var a = new FakeProviderA();
        var b = new FakeProviderB();
        var c = new FakeProviderC();
        var sender = CreateSender(a, b, c);

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
        var sender = CreateSender(a, b, c);

        await sender.SendAsync(TestMessage, CancellationToken.None);

        Assert.Equal(1, c.SendCount);
    }

    [Fact]
    public async Task SendAsync_RateLimitShouldUseCooldownFromException()
    {
        var (sender, a, b) = CreateSenderWithTwoProviders();
        a.ShouldRateLimit = true;
        b.ShouldRateLimit = true;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        a.ShouldRateLimit = false;
        b.ShouldRateLimit = false;

        // Both are on cooldown from the ProviderRateLimitException, no providers available
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(TestMessage, CancellationToken.None));

        Assert.Equal(1, a.SendCount);
        Assert.Equal(1, b.SendCount);
    }

    [Fact]
    public async Task SendAsync_MixedFailures_ShouldOnlyCooldownRateLimited()
    {
        var a = new FakeProviderA { ExceptionToThrow = new InvalidOperationException("boom") };
        var b = new FakeProviderB { ShouldRateLimit = true };
        var c = new FakeProviderC();
        var sender = CreateSender(a, b, c);

        await sender.SendAsync(TestMessage, CancellationToken.None);

        a.ExceptionToThrow = null;

        // A should still be available (wasn't rate-limited, just failed)
        // B should be on cooldown (was rate-limited)
        // C is always available
        for (var i = 0; i < 6; i++)
            await sender.SendAsync(TestMessage, CancellationToken.None);

        Assert.True(a.SendCount >= 2, "Provider A should still be available after non-rate-limit failure");
        Assert.True(b.SendCount == 1, "Provider B should be on cooldown after rate-limit");
    }

    private static (RoundRobinEmailSender Sender, FakeProviderA A, FakeProviderB B) CreateSenderWithTwoProviders()
    {
        var a = new FakeProviderA();
        var b = new FakeProviderB();
        var sender = CreateSender(a, b);

        return (sender, a, b);
    }

    private static RoundRobinEmailSender CreateSender(params IEmailProvider[] providers)
    {
        var logger = new NullLogger<RoundRobinEmailSender>();
        return new RoundRobinEmailSender(providers, logger);
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
                throw new ProviderRateLimitException(GetType().Name, TestCooldown);

            return Task.CompletedTask;
        }
    }

    private sealed class FakeProviderA : FakeProviderBase;

    private sealed class FakeProviderB : FakeProviderBase;

    private sealed class FakeProviderC : FakeProviderBase;
}
