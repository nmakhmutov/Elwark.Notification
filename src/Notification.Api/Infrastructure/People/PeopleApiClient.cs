using System.Net;

namespace Notification.Api.Infrastructure.People;

internal sealed class PeopleApiClient : IPeopleApiClient
{
    private readonly HttpClient _client;

    public PeopleApiClient(HttpClient client) =>
        _client = client;

    public async Task<AccountModel?> GetAccountAsync(long accountId, CancellationToken ct)
    {
        var response = await _client.GetAsync($"accounts/{accountId}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccountModel>(ct);
    }
}
