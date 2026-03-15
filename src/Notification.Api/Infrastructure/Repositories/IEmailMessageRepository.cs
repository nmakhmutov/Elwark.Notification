using Notification.Api.Models;

namespace Notification.Api.Infrastructure.Repositories;

public interface IEmailMessageRepository
{
    Task<EmailMessage> CreateAsync(EmailMessage entity, CancellationToken ct = default);

    Task<EmailMessage?> TryAcquireNextAsync(DateTime now, CancellationToken ct = default);

    Task RescheduleAsync(Guid id, DateTime sendAt, string? error, CancellationToken ct = default);

    Task CompleteAsync(Guid id, CancellationToken ct = default);
}
