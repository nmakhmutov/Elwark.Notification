namespace Notification.Api.Infrastructure;

public sealed record EmailRequest(string To, string Subject, string Body, bool IsHtml);
