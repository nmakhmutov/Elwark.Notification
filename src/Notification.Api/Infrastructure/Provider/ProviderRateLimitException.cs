namespace Notification.Api.Infrastructure.Provider;

public sealed class ProviderRateLimitException : Exception
{
    public string Provider { get; }

    public TimeSpan RetryAfter { get; }

    public ProviderRateLimitException(string provider, TimeSpan retryAfter)
        : base($"Rate limit exceeded for provider '{provider}', retry after {retryAfter}")
    {
        Provider = provider;
        RetryAfter = retryAfter;
    }
}
