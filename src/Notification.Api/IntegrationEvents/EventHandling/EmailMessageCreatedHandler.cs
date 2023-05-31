using System.Net.Mail;
using MongoDB.Driver;
using Notification.Api.Infrastructure.Provider;
using Notification.Api.Infrastructure.Provider.Gmail;
using Notification.Api.Infrastructure.Provider.SendGrid;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.Integration;
using Notification.Api.IntegrationEvents.Events;
using Notification.Api.Models;
using Polly;
using Polly.Retry;

namespace Notification.Api.IntegrationEvents.EventHandling;

internal sealed class EmailMessageCreatedHandler : IIntegrationEventHandler<EmailCreatedIntegrationEvent>
{
    private readonly AsyncRetryPolicy<IEmailSender?> _policy;
    private readonly IEmailProviderRepository _repository;
    private readonly IEnumerable<IEmailSender> _senders;

    public EmailMessageCreatedHandler(IEmailProviderRepository repository, IEnumerable<IEmailSender> senders)
    {
        _senders = senders;
        _repository = repository;
        _policy = Policy<IEmailSender?>.Handle<MongoException>()
            .RetryForeverAsync();
    }

    public async Task HandleAsync(EmailCreatedIntegrationEvent message)
    {
        var provider = await _policy.ExecuteAsync(DequeueEmailProviderAsync);
        if (provider is null)
            return;

        await provider.SendEmailAsync(new MailAddress(message.Email), message.Subject, message.Body);
    }

    private async Task<IEmailSender?> DequeueEmailProviderAsync()
    {
        var provider = await _repository.GetNextAsync();
        if (provider is null)
            return null;

        provider.DecreaseBalance();

        await _repository.UpdateAsync(provider);

        return _senders.FirstOrDefault(x => provider.Id switch
        {
            EmailProvider.Type.Sendgrid => x is SendgridProvider,
            EmailProvider.Type.Gmail => x is GmailProvider,
            _ => throw new ArgumentOutOfRangeException(nameof(provider.Id), "Unknown email provider")
        });
    }
}
