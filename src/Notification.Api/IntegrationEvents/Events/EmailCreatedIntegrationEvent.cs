using Notification.Api.Integration;

namespace Notification.Api.IntegrationEvents.Events;

public sealed record EmailCreatedIntegrationEvent(
    Guid MessageId,
    DateTime CreatedAt,
    string Email,
    string Subject,
    string Body
) : IIntegrationEvent;
