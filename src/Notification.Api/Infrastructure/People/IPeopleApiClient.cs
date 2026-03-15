namespace Notification.Api.Infrastructure.People;

internal interface IPeopleApiClient
{
    Task<AccountModel?> GetAccountAsync(long accountId, CancellationToken ct = default);
}

internal sealed record AccountModel(long Id, string Email, string Timezone);
