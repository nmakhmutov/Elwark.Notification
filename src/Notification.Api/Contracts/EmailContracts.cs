using Microsoft.AspNetCore.Mvc;

namespace Notification.Api.Contracts;

public sealed record EmailQueueReply(string Status, DateTime SendAt);

public sealed class SendToAddressRequest
{
    [FromRoute]
    public string Email { get; init; } = string.Empty;

    [FromQuery(Name = "subject")]
    public string Subject { get; init; } = string.Empty;
}

public sealed class ScheduleToAddressRequest
{
    [FromRoute]
    public string Email { get; init; } = string.Empty;

    [FromQuery(Name = "subject")]
    public string Subject { get; init; } = string.Empty;

    [FromQuery(Name = "timezone")]
    public string Timezone { get; init; } = string.Empty;
}

public sealed class SendToUserRequest
{
    [FromRoute]
    public long UserId { get; init; }

    [FromQuery(Name = "subject")]
    public string Subject { get; init; } = string.Empty;
}

public sealed class ScheduleToUserRequest
{
    [FromRoute]
    public long UserId { get; init; }

    [FromQuery(Name = "subject")]
    public string Subject { get; init; } = string.Empty;
}
