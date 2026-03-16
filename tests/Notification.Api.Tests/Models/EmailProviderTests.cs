using Notification.Api.Models;

namespace Notification.Api.Tests.Models;

public sealed class EmailProviderTests
{
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
    public void DecreaseBalance_WhenBalanceIsZero_ShouldThrow()
    {
        var provider = new Sendgrid(100, 0);

        Assert.Throws<Exception>(() => provider.DecreaseBalance());
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
    public void Sendgrid_UpdateBalance_WhenSameDay_ShouldNotReset()
    {
        var provider = new Sendgrid(100, 10);
        var balanceBefore = provider.Balance;

        provider.UpdateBalance();

        Assert.Equal(balanceBefore, provider.Balance);
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
    public void Sendgrid_UpdateBalance_WhenDifferentDay_ShouldAdvanceResetAt()
    {
        var provider = new Sendgrid(100, 10);
        var resetAtBefore = provider.ResetAt;
        SetUpdatedAt(provider, DateTime.UtcNow.AddDays(-1));

        provider.UpdateBalance();

        Assert.True(provider.ResetAt > resetAtBefore);
    }

    [Fact]
    public void Resend_UpdateBalance_WhenSameDay_ShouldNotReset()
    {
        var provider = new Notification.Api.Models.Resend(200, 15);
        var balanceBefore = provider.Balance;

        provider.UpdateBalance();

        Assert.Equal(balanceBefore, provider.Balance);
    }

    [Fact]
    public void Resend_UpdateBalance_WhenDifferentDay_ShouldResetToLimit()
    {
        var provider = new Notification.Api.Models.Resend(200, 15);
        SetUpdatedAt(provider, DateTime.UtcNow.AddDays(-1));

        provider.UpdateBalance();

        Assert.Equal(200, provider.Balance);
    }

    private static void SetUpdatedAt(EmailProvider provider, DateTime value) =>
        typeof(EmailProvider)
            .GetProperty(nameof(EmailProvider.UpdatedAt))!
            .SetValue(provider, value);
}
