using System.Net;
using System.Net.Mail;

namespace Notification.Api.Infrastructure.Provider;

internal sealed partial class GmailEmailProvider : IEmailProvider
{
    private readonly ILogger<GmailEmailProvider> _logger;
    private readonly string _password;
    private readonly string _username;

    public GmailEmailProvider(string username, string password, ILogger<GmailEmailProvider> logger)
    {
        _username = username;
        _password = password;
        _logger = logger;
    }

    public async Task SendAsync(EmailRequest message, CancellationToken ct)
    {
        var mail = new MailMessage
        {
            From = new MailAddress("noreply@elwark.app", "Elwark"),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml,
            To = { message.To }
        };

        using var smtp = new SmtpClient("smtp.gmail.com", 587);
        smtp.Credentials = new NetworkCredential(_username, _password);
        smtp.EnableSsl = true;

        try
        {
            await smtp.SendMailAsync(mail, ct);
            LogEmailSend(message.To, message.Subject);
        }
        catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.ServiceNotAvailable)
        {
            throw new ProviderRateLimitException("Gmail");
        }
        catch (SmtpException ex)
        {
            LogEmailFailed(message.To, message.Subject, ex.StatusCode, ex.Message);
            throw new InvalidOperationException($"Gmail SMTP error {ex.StatusCode}: {ex.Message}", ex);
        }
    }

    [LoggerMessage(LogLevel.Information, "Email to {email} with subject {subject} sent via Gmail")]
    partial void LogEmailSend(string email, string subject);

    [LoggerMessage(LogLevel.Error,
        "Failed to send email to {email} with subject {subject} via Gmail: {code}, Message: {message}")]
    partial void LogEmailFailed(string email, string subject, SmtpStatusCode code, string message);
}
