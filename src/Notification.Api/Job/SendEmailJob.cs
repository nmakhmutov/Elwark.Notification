using Notification.Api.Extensions;
using Notification.Api.Infrastructure;
using Notification.Api.Infrastructure.Repositories;
using Quartz;

namespace Notification.Api.Job;

[DisallowConcurrentExecution]
internal sealed class SendEmailJob : IJob
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<SendEmailJob> _logger;
    private readonly IEmailSender _sender;

    public SendEmailJob(IServiceScopeFactory factory, IEmailSender sender, ILogger<SendEmailJob> logger)
    {
        _factory = factory;
        _sender = sender;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = context.FireTimeUtc.UtcDateTime.CeilingToMinute();

        await using var scope = _factory.CreateAsyncScope();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();

        while (await emailRepository.TryAcquireNextAsync(now, context.CancellationToken) is { } email)
        {
            try
            {
                var message = new EmailRequest(email.Email, email.Subject, email.Body, email.IsHtml);
                await _sender.SendAsync(message, context.CancellationToken);

                await emailRepository.CompleteAsync(email.Id, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queued email {EmailId}", email.Id);

                await emailRepository.RescheduleAsync(
                    email.Id,
                    now.Add(RetryDelay),
                    ex.Message,
                    context.CancellationToken
                );
            }
        }
    }
}
