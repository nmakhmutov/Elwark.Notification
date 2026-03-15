using Microsoft.EntityFrameworkCore;
using Notification.Api.Models;

namespace Notification.Api.Infrastructure;

internal sealed class NotificationDbContextSeed
{
    private readonly NotificationDbContext _dbContext;

    public NotificationDbContextSeed(NotificationDbContext dbContext) =>
        _dbContext = dbContext;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (!await _dbContext.EmailProviders.AnyAsync(x => x.Id == EmailProvider.Type.Resend, ct))
            _dbContext.EmailProviders.Add(new Models.Resend(100, 100));

        if (!await _dbContext.EmailProviders.AnyAsync(x => x.Id == EmailProvider.Type.Sendgrid, ct))
            _dbContext.EmailProviders.Add(new Sendgrid(100, 100));

        await _dbContext.SaveChangesAsync(ct);
    }
}
