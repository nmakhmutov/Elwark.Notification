using System.Net;
using Microsoft.Extensions.Options;
using Resend;

namespace Notification.Api.Infrastructure.Provider;

internal sealed partial class ResendEmailProvider : IEmailProvider
{
    private static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(10);

    private readonly IHttpClientFactory _factory;
    private readonly IOptionsMonitor<ResendClientOptions> _options;
    private readonly ILogger<ResendEmailProvider> _logger;

    public ResendEmailProvider(IHttpClientFactory factory, IOptionsMonitor<ResendClientOptions> options, ILogger<ResendEmailProvider> logger)
    {
        _factory = factory;
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(EmailRequest message, CancellationToken ct)
    {
        var httpClient = _factory.CreateClient(nameof(ResendClient));
        var resend = ResendClient.Create(_options.CurrentValue, httpClient);

        var msg = new EmailMessage
        {
            From = "Elwark <noreply@elwark.app>",
            Subject = message.Subject,
            HtmlBody = message.IsHtml ? message.Body : null,
            TextBody = message.IsHtml ? null : message.Body,
            To = new[] { message.To }
        };

        try
        {
            var response = await resend.EmailSendAsync(msg, ct);

            LogEmailSend(message.To, message.Subject, response.Success);
            LogRateLimit(response.Limits?.Limit, response.Limits?.Remaining, response.Limits?.RetryAfter);
        }
        catch (ResendException ex)
        {
            if (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = ex.Limits?.RetryAfter is > 0
                    ? TimeSpan.FromSeconds(ex.Limits.RetryAfter.Value)
                    : DefaultCooldown;

                throw new ProviderRateLimitException("Resend", retryAfter);
            }

            LogEmailFailed(message.To, message.Subject, ex.StatusCode, ex.Message);
            LogRateLimit(ex.Limits?.Limit, ex.Limits?.Remaining, ex.Limits?.RetryAfter);

            throw new InvalidOperationException($"Resend returned {ex.StatusCode}: {ex.Message}", ex);
        }
    }

    [LoggerMessage(LogLevel.Information, "Email to {email} with subject {subject} sent via Resend: {success}")]
    partial void LogEmailSend(string email, string subject, bool success);

    [LoggerMessage(LogLevel.Error, "Failed to send email to {email} with subject {subject} via Resend: {code}, Message: {message}")]
    partial void LogEmailFailed(string email, string subject, HttpStatusCode? code, string message);

    [LoggerMessage(LogLevel.Information, "Resend RateLimit: {limit}, Remaining: {remaining}, RetryAfter: {retryAfter}")]
    partial void LogRateLimit(int? limit, int? remaining, int? retryAfter);
}
