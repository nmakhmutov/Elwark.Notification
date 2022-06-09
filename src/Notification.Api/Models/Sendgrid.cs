namespace Notification.Api.Models;

public sealed class Sendgrid : EmailProvider
{
    public Sendgrid(int limit, int balance)
        : base(Type.Sendgrid, limit, balance)
    {
        var date = DateTime.Today;
        UpdateAt = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc).AddDays(1);
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
