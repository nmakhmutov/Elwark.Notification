namespace Notification.Api.Models;

public abstract class EmailProvider
{
    public enum Type
    {
        Sendgrid = 1,
        Resend = 2
    }

    protected EmailProvider()
    {
    }

    protected EmailProvider(Type id, int limit, int balance)
    {
        Id = id;
        Limit = limit;
        Balance = balance;
        IsEnabled = true;
        Version = 0;
        UpdateAt = DateOnly.MinValue;
        UpdatedAt = DateTime.MinValue;
    }

    public Type Id { get; protected set; }

    public int Limit { get; protected set; }

    public int Balance { get; protected set; }

    public bool IsEnabled { get; protected set; }

    public DateOnly UpdateAt { get; protected set; }

    public DateTime UpdatedAt { get; protected set; }

    public uint Version { get; set; }

    public abstract void UpdateBalance();

    public void DecreaseBalance()
    {
        if (Balance == 0)
            throw new Exception($"'{Id}' balance is empty");

        Balance--;
        UpdatedAt = DateTime.UtcNow;
    }
}
