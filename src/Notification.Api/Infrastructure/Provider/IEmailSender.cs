using System.Net.Mail;

namespace Notification.Api.Infrastructure.Provider;

public interface IEmailSender
{
    public Task SendAsync(MailAddress email, string subject, string body, bool isHtml, CancellationToken ct = default);
}
