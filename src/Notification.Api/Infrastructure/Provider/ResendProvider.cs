using System.Net.Mail;
using Resend;

namespace Notification.Api.Infrastructure.Provider;

public sealed class ResendProvider : IEmailSender
{
    private readonly IResend _resend;
    private readonly ILogger<ResendProvider> _logger;

    public ResendProvider(IResend resend, ILogger<ResendProvider> logger)
    {
        _resend = resend;
        _logger = logger;
    }

    public async Task SendAsync(MailAddress email, string subject, string body, bool isHtml, CancellationToken ct)
    {
        var message = new EmailMessage
        {
            From = "Elwark <notification@elwark.com>",
            Subject = subject,
            HtmlBody = isHtml ? body : null,
            TextBody = isHtml ? null : body
        };
        message.To.Add(email.Address);

        var response = await _resend.EmailSendAsync(message, ct);

        _logger.LogInformation("Email to {Email} with subject {Subject} sent: {Code}",
            email.Address, subject, response.Success);
    }
}
