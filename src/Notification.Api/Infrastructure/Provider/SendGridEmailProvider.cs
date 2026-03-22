using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Notification.Api.Infrastructure.Provider;

internal sealed partial class SendGridEmailProvider : IEmailProvider
{
    private static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(10);

    private readonly IHttpClientFactory _factory;
    private readonly string _apiKey;
    private readonly ILogger<SendGridEmailProvider> _logger;

    public SendGridEmailProvider(IHttpClientFactory factory, string apiKey, ILogger<SendGridEmailProvider> logger)
    {
        _factory = factory;
        _apiKey = apiKey;
        _logger = logger;
    }

    public async Task SendAsync(EmailRequest message, CancellationToken ct)
    {
        var httpClient = _factory.CreateClient(nameof(SendGridClient));
        var client = new SendGridClient(httpClient, _apiKey);

        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress("elwarkinc@gmail.com", "Elwark"),
            new EmailAddress(message.To),
            message.Subject,
            message.IsHtml ? null : message.Body,
            message.IsHtml ? message.Body : null
        );

        var response = await client.SendEmailAsync(msg, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new ProviderRateLimitException("SendGrid", DefaultCooldown);

        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            LogEmailSend(message.To, message.Subject);
            return;
        }

        var body = await response.Body.ReadAsStringAsync(ct);
        LogEmailFailed(message.To, message.Subject, response.StatusCode, body);

        throw new InvalidOperationException($"SendGrid returned {response.StatusCode}: {body}");
    }

    [LoggerMessage(LogLevel.Information, "Email to {email} with subject {subject} sent via SendGrid")]
    partial void LogEmailSend(string email, string subject);

    [LoggerMessage(LogLevel.Error, "Failed to send email to {email} with subject {subject} via SendGrid: {code}, Body: {body}")]
    partial void LogEmailFailed(string email, string subject, HttpStatusCode code, string body);
}
