namespace Notification.Api.Models;

public sealed class Resend : EmailProvider
{
    private Resend()
    {
    }

    public Resend(int limit, int balance)
        : base(Type.Resend, limit, balance)
    {
        UpdateAt = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        UpdatedAt = DateTime.UtcNow;
    }

    public override void UpdateBalance()
    {
        if (DateTime.UtcNow.Date == UpdatedAt.Date)
            return;

        Balance = Limit;
        UpdateAt = UpdateAt.AddDays(1);
        UpdatedAt = DateTime.UtcNow;
    }
}
