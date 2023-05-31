namespace Notification.Api.Models;

public sealed class Gmail : EmailProvider
{
    public Gmail(uint limit, uint balance)
        : base(Type.Gmail, limit, balance)
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
