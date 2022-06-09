using MongoDB.Driver;
using Notification.Api.Infrastructure;
using Notification.Api.Integration;
using Notification.Api.IntegrationEvents.Events;
using Notification.Api.Models;
using Quartz;

namespace Notification.Api.Job;

[DisallowConcurrentExecution]
public sealed class PostponedEmailJob : IJob
{
    private readonly IIntegrationEventBus _bus;
    private readonly NotificationDbContext _dbContext;

    public PostponedEmailJob(IIntegrationEventBus bus, NotificationDbContext dbContext)
    {
        _bus = bus;
        _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = context.FireTimeUtc.UtcDateTime;

        using var cursor = await _dbContext.PostponedEmails
            .Find(Builders<PostponedEmail>.Filter.Lt(x => x.SendAt, now))
            .Sort(Builders<PostponedEmail>.Sort.Descending(x => x.SendAt))
            .ToCursorAsync();

        while (await cursor.MoveNextAsync(context.CancellationToken))
            foreach (var email in cursor.Current)
            {
                var evt = new EmailCreatedIntegrationEvent(Guid.NewGuid(), now, email.Email, email.Subject, email.Body);
                await _bus.PublishAsync(evt, context.CancellationToken);

                await _dbContext.PostponedEmails.DeleteOneAsync(x => x.Id == email.Id, context.CancellationToken);
            }
    }
}
