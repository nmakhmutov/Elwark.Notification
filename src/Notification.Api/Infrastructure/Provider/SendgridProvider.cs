using System.Net.Mail;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Notification.Api.Infrastructure.Provider;

// ReSharper disable NotAccessedPositionalProperty.Local
internal sealed class SendgridProvider : IEmailSender
{
    private readonly SendGridClient _client;
    private readonly ILogger<SendgridProvider> _logger;

    public SendgridProvider(SendGridClient client, ILogger<SendgridProvider> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendAsync(MailAddress email, string subject, string body, bool isHtml, CancellationToken ct)
    {
        var message = MailHelper.CreateSingleEmail(
            new EmailAddress("elwarkinc@gmail.com", "Elwark"),
            new EmailAddress(email.Address, email.DisplayName),
            subject,
            isHtml ? null : body,
            isHtml ? body : null
        );

        var response = await _client.SendEmailAsync(message, ct);

        _logger.LogInformation("Email to {Email} with subject {Subject} sent: {Code}",
            email.Address, subject, response.StatusCode);
    }
}
