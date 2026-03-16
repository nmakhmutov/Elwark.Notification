using Microsoft.EntityFrameworkCore;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.Models;
using Polly;
using Polly.Retry;
using Quartz;

namespace Notification.Api.Job;

[DisallowConcurrentExecution]
internal sealed partial class UpdateProviderBalanceJob : IJob
{
    private static readonly AsyncRetryPolicy RetryPolicy = Policy
        .Handle<DbUpdateException>()
        .Or<DbUpdateConcurrencyException>()
        .RetryForeverAsync();

    private readonly IServiceScopeFactory _factory;
    private readonly ILogger<UpdateProviderBalanceJob> _logger;

    public UpdateProviderBalanceJob(IServiceScopeFactory factory, ILogger<UpdateProviderBalanceJob> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await using var scope = _factory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailProviderRepository>();

        foreach (var id in Enum.GetValues<EmailProvider.Type>())
        {
            await RetryPolicy.ExecuteAsync(async () =>
            {
                var provider = await repository.GetAsync(id, context.CancellationToken);
                if (provider is null)
                    return;

                LogProviderBalanceUpdating(provider.Id, provider.Balance, provider.Limit);

                provider.UpdateBalance();
                await repository.UpdateAsync(provider, context.CancellationToken);
            });
        }
    }

    [LoggerMessage(LogLevel.Information, "Provider {Provider} balance {Balance} updating with limit {Limit}")]
    partial void LogProviderBalanceUpdating(EmailProvider.Type provider, int balance, int limit);
}
