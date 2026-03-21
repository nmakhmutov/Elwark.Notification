using System.Net;
using Resend;

namespace Notification.Api.Infrastructure.Provider;

internal sealed partial class ResendEmailProvider : IEmailProvider
{
    private readonly ILogger<ResendEmailProvider> _logger;
    private readonly IResend _resend;

    public ResendEmailProvider(IResend resend, ILogger<ResendEmailProvider> logger)
    {
        _resend = resend;
        _logger = logger;
    }

    public async Task SendAsync(EmailRequest message, CancellationToken ct)
    {
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
            var response = await _resend.EmailSendAsync(msg, ct);

            LogEmailSend(message.To, message.Subject, response.Success);
            LogRateLimit(response.Limits?.Limit, response.Limits?.Remaining, response.Limits?.RetryAfter);
        }
        catch (ResendException ex)
        {
            if (ex.StatusCode == HttpStatusCode.TooManyRequests)
                throw new ProviderRateLimitException("Resend");

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
