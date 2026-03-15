using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Notification.Api.Infrastructure.Provider;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.Models;
using Polly;
using Polly.Retry;
using Quartz;

namespace Notification.Api.Job;

[DisallowConcurrentExecution]
internal sealed class SendEmailJob : IJob
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    private static readonly AsyncRetryPolicy RetryPolicy = Policy
        .Handle<DbUpdateException>()
        .Or<DbUpdateConcurrencyException>()
        .RetryForeverAsync();

    private readonly ILogger<SendEmailJob> _logger;
    private readonly IEnumerable<IEmailSender> _senders;
    private readonly IServiceScopeFactory _factory;

    public SendEmailJob(IServiceScopeFactory factory, IEnumerable<IEmailSender> senders, ILogger<SendEmailJob> logger)
    {
        _factory = factory;
        _senders = senders;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = context.FireTimeUtc.UtcDateTime;
        await using var scope = _factory.CreateAsyncScope();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IEmailProviderRepository>();
        var emailRepository = scope.ServiceProvider.GetRequiredService<IEmailMessageRepository>();

        while (await emailRepository.TryAcquireNextAsync(now, context.CancellationToken) is { } email)
        {
            var provider = await DequeueProviderAsync(providerRepository, context.CancellationToken);
            if (provider is null)
            {
                await emailRepository.RescheduleAsync(
                    email.Id,
                    now.Add(RetryDelay),
                    "No email provider is not available",
                    context.CancellationToken
                );

                continue;
            }

            try
            {
                await provider.SendAsync(
                    new MailAddress(email.Email),
                    email.Subject,
                    email.Body,
                    email.IsHtml,
                    context.CancellationToken
                );

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

    private async Task<IEmailSender?> DequeueProviderAsync(IEmailProviderRepository repository, CancellationToken ct)
    {
        var provider = await RetryPolicy.ExecuteAsync(async () =>
        {
            var result = await repository.GetNextAsync(ct);
            if (result is null)
                return null;

            result.DecreaseBalance();
            await repository.UpdateAsync(result, ct);

            return result;
        });

        if (provider is null)
            return null;

        return provider.Id switch
        {
            EmailProvider.Type.Sendgrid => _senders.OfType<SendgridProvider>().FirstOrDefault(),
            EmailProvider.Type.Resend => _senders.OfType<ResendProvider>().FirstOrDefault(),
            _ => null
        };
    }
}
