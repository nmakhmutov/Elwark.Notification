namespace Notification.Api.Infrastructure.Provider;

public sealed class ProviderRateLimitException : Exception
{
    public string Provider { get; }

    public ProviderRateLimitException(string provider)
        : base($"Rate limit exceeded for provider '{provider}'") =>
        Provider = provider;
}
