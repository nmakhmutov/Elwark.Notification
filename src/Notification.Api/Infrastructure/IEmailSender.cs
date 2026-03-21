namespace Notification.Api.Infrastructure;

public interface IEmailSender
{
    Task SendAsync(EmailRequest message, CancellationToken ct = default);
}
