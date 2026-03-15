using Microsoft.EntityFrameworkCore;
using Notification.Api.Infrastructure;
using Notification.Api.Models;
using Quartz;

namespace Notification.Api.Job;

[DisallowConcurrentExecution]
internal sealed class DeleteCompletedEmailsJob : IJob
{
    private readonly IDbContextFactory<NotificationDbContext> _factory;
    private readonly ILogger<DeleteCompletedEmailsJob> _logger;

    public DeleteCompletedEmailsJob(IDbContextFactory<NotificationDbContext> factory,
        ILogger<DeleteCompletedEmailsJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await _factory.CreateDbContextAsync();

        var deleted = await dbContext.TempEmails
            .Where(x => x.Status == EmailMessage.QueueStatus.Completed)
            .ExecuteDeleteAsync(context.CancellationToken);

        if (deleted > 0)
            _logger.LogInformation("Deleted {Count} completed email messages", deleted);
    }
}
