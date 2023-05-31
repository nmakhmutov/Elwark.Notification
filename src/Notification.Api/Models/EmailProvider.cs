// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Notification.Api.Models;

public abstract class EmailProvider
{
    public enum Type
    {
        Sendgrid = 1,
        Gmail = 2
    }

    protected EmailProvider(Type type, uint limit, uint balance)
    {
        Id = type;
        Limit = limit;
        Balance = balance;
        IsEnabled = true;
        Version = uint.MinValue;
        UpdateAt = DateOnly.MinValue;
        UpdatedAt = DateTime.MinValue;
    }

    public Type Id { get; protected set; }

    public uint Version { get; set; }

    public uint Limit { get; protected set; }

    public uint Balance { get; protected set; }

    public DateOnly UpdateAt { get; protected set; }

    public DateTime UpdatedAt { get; protected set; }

    public bool IsEnabled { get; protected set; }

    public abstract void UpdateBalance();

    public void DecreaseBalance()
    {
        if (Balance == 0)
            throw new Exception($"'{Id}' balance is empty");

        Balance--;
        UpdatedAt = DateTime.UtcNow;
    }
}
