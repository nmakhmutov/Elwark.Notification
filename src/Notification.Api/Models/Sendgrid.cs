namespace Notification.Api.Models;

public sealed class Sendgrid : EmailProvider
{
    public Sendgrid(uint limit, uint balance)
        : base(Type.Sendgrid, limit, balance)
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
