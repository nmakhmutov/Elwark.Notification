using Microsoft.EntityFrameworkCore;
using Notification.Api.Models;

namespace Notification.Api.Infrastructure.Repositories;

internal sealed class EmailProviderRepository : IEmailProviderRepository
{
    private readonly NotificationDbContext _dbContext;

    public EmailProviderRepository(NotificationDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task<EmailProvider?> GetNextAsync(CancellationToken ct) =>
        await _dbContext.EmailProviders
            .Where(x => x.Balance > 0 && x.IsEnabled)
            .OrderByDescending(x => x.Balance)
            .FirstOrDefaultAsync(ct);

    public async Task<EmailProvider?> GetAsync(EmailProvider.Type key, CancellationToken ct) =>
        await _dbContext.EmailProviders
            .FirstOrDefaultAsync(x => x.Id == key, ct);

    public async Task UpdateAsync(EmailProvider entity, CancellationToken ct)
    {
        _dbContext.EmailProviders.Update(entity);
        await _dbContext.SaveChangesAsync(ct);
    }
}
