using Microsoft.AspNetCore.Http.HttpResults;
using Notification.Api.Contracts;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.Infrastructure.Validation;
using Notification.Api.Models;
using Notification.Api.Scheduling;

namespace Notification.Api.Endpoints;

internal static class EmailEndpoints
{
    public static IEndpointRouteBuilder MapEmailEndpoints(this IEndpointRouteBuilder app)
    {
        var emails = app.MapGroup("/emails/{email}")
            .WithTags("Emails")
            .RequireAuthorization();

        emails.MapPost("/", SendEmailAsync)
            .AddEndpointFilter<ValidationFilter<SendToAddressRequest>>();

        emails.MapPost("/schedule", ScheduleEmailAsync)
            .AddEndpointFilter<ValidationFilter<ScheduleToAddressRequest>>();

        return app;
    }

    private static Task<Results<Accepted<EmailQueueReply>, BadRequest<string>>> SendEmailAsync(
        [AsParameters] SendToAddressRequest request,
        IEmailMessageRepository repository,
        HttpRequest httpRequest,
        CancellationToken ct
    )
    {
        return QueueEmailAsync(request.Email, request.Subject, null, httpRequest, repository, ct);
    }

    private static Task<Results<Accepted<EmailQueueReply>, BadRequest<string>>> ScheduleEmailAsync(
        [AsParameters] ScheduleToAddressRequest request,
        IEmailMessageRepository repository,
        HttpRequest httpRequest,
        CancellationToken ct
    )
    {
        return QueueEmailAsync(request.Email, request.Subject, request.Timezone, httpRequest, repository, ct);
    }

    private static async Task<Results<Accepted<EmailQueueReply>, BadRequest<string>>> QueueEmailAsync(
        string email,
        string subject,
        string? timezone,
        HttpRequest request,
        IEmailMessageRepository repository,
        CancellationToken ct
    )
    {
        var isHtml = GetIsHtml(request.ContentType);
        if (!isHtml.HasValue)
            return TypedResults.BadRequest("Supported content types are text/plain and text/html");

        var body = await ReadBodyAsync(request, ct);
        if (string.IsNullOrWhiteSpace(body))
            return TypedResults.BadRequest("Request body is required.");

        var now = DateTime.UtcNow;
        var sendAt = EmailScheduleCalculator.CalculateSendAt(now, timezone);

        var message = EmailMessage.Create(email, subject, body, isHtml.Value, sendAt);
        await repository.CreateAsync(message, ct);

        var status = sendAt <= now ? "queued" : "postponed";
        return TypedResults.Accepted(string.Empty, new EmailQueueReply(status, message.SendAt));
    }

    private static bool? GetIsHtml(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return null;

        var mediaType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        if (mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
            return true;

        if (mediaType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }

    private static async Task<string> ReadBodyAsync(HttpRequest request, CancellationToken ct)
    {
        using var reader = new StreamReader(request.Body);
        return await reader.ReadToEndAsync(ct);
    }
}
