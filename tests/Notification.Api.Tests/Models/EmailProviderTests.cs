using Notification.Api.Models;

namespace Notification.Api.Tests.Models;

public sealed class EmailProviderTests
{
    // ── DecreaseBalance ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void DecreaseBalance_ShouldDecrementByOne(int initialBalance)
    {
        var provider = new Sendgrid(100, initialBalance);

        provider.DecreaseBalance();

        Assert.Equal(initialBalance - 1, provider.Balance);
    }

    [Fact]
    public void DecreaseBalance_FromOneToZero_ShouldSucceed()
    {
        var provider = new Sendgrid(100, 1);

        provider.DecreaseBalance();

        Assert.Equal(0, provider.Balance);
    }

    [Fact]
    public void DecreaseBalance_WhenBalanceIsZero_ShouldThrow()
    {
        var provider = new Sendgrid(100, 0);

        Assert.Throws<Exception>(() => provider.DecreaseBalance());
    }

    [Fact]
    public void DecreaseBalance_ExceptionMessage_ShouldMentionProvider()
    {
        var provider = new Sendgrid(100, 0);

        var ex = Assert.Throws<Exception>(() => provider.DecreaseBalance());

        Assert.Contains("Sendgrid", ex.Message);
    }

    [Fact]
    public void DecreaseBalance_ShouldUpdateUpdatedAt()
    {
        var provider = new Sendgrid(100, 10);
        var before = DateTime.UtcNow;

        provider.DecreaseBalance();

        Assert.True(provider.UpdatedAt >= before);
    }

    [Fact]
    public void DecreaseBalance_CalledMultipleTimes_ShouldAccumulateDecrements()
    {
        var provider = new Sendgrid(100, 5);

        provider.DecreaseBalance();
        provider.DecreaseBalance();
        provider.DecreaseBalance();

        Assert.Equal(2, provider.Balance);
    }

    // ── Sendgrid construction ─────────────────────────────────────────────────

    [Fact]
    public void Sendgrid_Constructor_ShouldSetCorrectProviderType()
    {
        var provider = new Sendgrid(100, 50);

        Assert.Equal(EmailProvider.Type.Sendgrid, provider.Id);
    }

    [Fact]
    public void Sendgrid_Constructor_ShouldSetIsEnabledTrue()
    {
        var provider = new Sendgrid(100, 50);

        Assert.True(provider.IsEnabled);
    }

    [Fact]
    public void Sendgrid_Constructor_ResetAtShouldBeInTheFuture()
    {
        var before = DateTime.UtcNow;

        var provider = new Sendgrid(100, 50);

        Assert.True(provider.ResetAt > before);
    }

    [Fact]
    public void Sendgrid_Constructor_VersionShouldBeZero()
    {
        var provider = new Sendgrid(100, 50);

        Assert.Equal(0u, provider.Version);
    }

    // ── Sendgrid UpdateBalance ─────────────────────────────────────────────────

    [Fact]
    public void Sendgrid_UpdateBalance_WhenSameDay_ShouldNotReset()
    {
        var provider = new Sendgrid(100, 10);

        provider.UpdateBalance();

        Assert.Equal(10, provider.Balance);
    }

    [Fact]
    public void Sendgrid_UpdateBalance_WhenDifferentDay_ShouldResetToLimit()
    {
        var provider = new Sendgrid(100, 10);
        SetUpdatedAt(provider, DateTime.UtcNow.AddDays(-1));

        provider.UpdateBalance();

        Assert.Equal(100, provider.Balance);
    }

    [Fact]
    public void Sendgrid_UpdateBalance_WhenZeroBalance_DifferentDay_ShouldResetToLimit()
    {
        var provider = new Sendgrid(100, 0);
        SetUpdatedAt(provider, DateTime.UtcNow.AddDays(-1));

        provider.UpdateBalance();

        Assert.Equal(100, provider.Balance);
    }

    [Fact]
    public void Sendgrid_UpdateBalance_WhenDifferentDay_ShouldAdvanceResetAt()
    {
        var provider = new Sendgrid(100, 10);
        var resetAtBefore = provider.ResetAt;
        SetUpdatedAt(provider, DateTime.UtcNow.AddDays(-1));

        provider.UpdateBalance();

        Assert.True(provider.ResetAt > resetAtBefore);
    }

    [Fact]
    public void Sendgrid_UpdateBalance_CalledMultipleTimes_SameDay_ShouldBeIdempotent()
    {
        var provider = new Sendgrid(100, 10);

        provider.UpdateBalance();
        provider.UpdateBalance();
        provider.UpdateBalance();

        Assert.Equal(10, provider.Balance);
    }

    // ── Resend construction ───────────────────────────────────────────────────

    [Fact]
    public void Resend_Constructor_ShouldSetCorrectProviderType()
    {
        var provider = new Notification.Api.Models.Resend(200, 100);

        Assert.Equal(EmailProvider.Type.Resend, provider.Id);
    }

    [Fact]
    public void Resend_Constructor_ShouldSetIsEnabledTrue()
    {
        var provider = new Notification.Api.Models.Resend(200, 100);

        Assert.True(provider.IsEnabled);
    }

    // ── Resend UpdateBalance ──────────────────────────────────────────────────

    [Fact]
    public void Resend_UpdateBalance_WhenSameDay_ShouldNotReset()
    {
        var provider = new Notification.Api.Models.Resend(200, 15);

        provider.UpdateBalance();

        Assert.Equal(15, provider.Balance);
    }

    [Fact]
    public void Resend_UpdateBalance_WhenDifferentDay_ShouldResetToLimit()
    {
        var provider = new Notification.Api.Models.Resend(200, 15);
        SetUpdatedAt(provider, DateTime.UtcNow.AddDays(-1));

        provider.UpdateBalance();

        Assert.Equal(200, provider.Balance);
    }

    [Fact]
    public void Resend_UpdateBalance_WhenDifferentDay_ShouldAdvanceResetAt()
    {
        var provider = new Notification.Api.Models.Resend(200, 15);
        var resetAtBefore = provider.ResetAt;
        SetUpdatedAt(provider, DateTime.UtcNow.AddDays(-1));

        provider.UpdateBalance();

        Assert.True(provider.ResetAt > resetAtBefore);
    }

    [Fact]
    public void Resend_UpdateBalance_CalledMultipleTimes_SameDay_ShouldBeIdempotent()
    {
        var provider = new Notification.Api.Models.Resend(200, 15);

        provider.UpdateBalance();
        provider.UpdateBalance();

        Assert.Equal(15, provider.Balance);
    }

    private static void SetUpdatedAt(EmailProvider provider, DateTime value) =>
        typeof(EmailProvider)
            .GetProperty(nameof(EmailProvider.UpdatedAt))!
            .SetValue(provider, value);
}
