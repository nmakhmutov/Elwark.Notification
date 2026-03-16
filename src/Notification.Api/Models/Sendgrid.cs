using Notification.Api.Extensions;

namespace Notification.Api.Models;

public sealed class Sendgrid : EmailProvider
{
    private Sendgrid()
    {
    }

    public Sendgrid(int limit, int balance)
        : base(Type.Sendgrid, limit, balance)
    {
        ResetAt = DateTime.UtcNow.AddDays(1).TruncateToMinute();
        UpdatedAt = DateTime.UtcNow;
    }

    public override void UpdateBalance()
    {
        if (DateTime.UtcNow.Date == UpdatedAt.Date)
            return;

        Balance = Limit;
        ResetAt = ResetAt.AddDays(1).TruncateToMinute();
        UpdatedAt = DateTime.UtcNow;
    }
}
