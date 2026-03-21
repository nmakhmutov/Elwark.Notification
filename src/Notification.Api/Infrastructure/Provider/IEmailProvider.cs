namespace Notification.Api.Infrastructure.Provider;

public interface IEmailProvider
{
    Task SendAsync(EmailRequest message, CancellationToken ct = default);
}
