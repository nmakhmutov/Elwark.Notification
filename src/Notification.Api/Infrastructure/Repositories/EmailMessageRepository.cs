using Microsoft.EntityFrameworkCore;
using Notification.Api.Models;

namespace Notification.Api.Infrastructure.Repositories;

internal sealed class EmailMessageRepository : IEmailMessageRepository
{
    private static readonly TimeSpan StaleLockTimeout = TimeSpan.FromMinutes(5);
    private readonly NotificationDbContext _dbContext;

    public EmailMessageRepository(NotificationDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<EmailMessage> CreateAsync(EmailMessage entity, CancellationToken ct)
    {
        await _dbContext.TempEmails.AddAsync(entity, ct);
        await _dbContext.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<EmailMessage?> TryAcquireNextAsync(DateTime now, CancellationToken ct)
    {
        var staleAt = now.Subtract(StaleLockTimeout);

        var email = await _dbContext.TempEmails
            .OrderBy(x => x.SendAt)
            .FirstOrDefaultAsync(x =>
                    (x.Status == EmailMessage.QueueStatus.Pending && x.SendAt <= now) ||
                    (x.Status == EmailMessage.QueueStatus.Processing && x.UpdatedAt <= staleAt),
                ct
            );

        if (email is null)
            return null;

        email.MarkProcessing(now);
        await _dbContext.SaveChangesAsync(ct);

        return email;
    }

    public async Task RescheduleAsync(Guid id, DateTime sendAt, string? error, CancellationToken ct)
    {
        var email = await _dbContext.TempEmails
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (email is null)
            return;

        email.Reschedule(sendAt, error);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(Guid id, CancellationToken ct)
    {
        var email = await _dbContext.TempEmails
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (email is null)
            return;

        email.MarkCompleted();
        await _dbContext.SaveChangesAsync(ct);
    }
}
