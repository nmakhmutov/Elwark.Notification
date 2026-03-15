using Microsoft.AspNetCore.Http.HttpResults;
using Notification.Api.Contracts;
using Notification.Api.Infrastructure.People;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.Infrastructure.Validation;
using Notification.Api.Models;
using Notification.Api.Scheduling;

namespace Notification.Api.Endpoints;

internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/users/{userId:long}")
            .WithTags("Users")
            .RequireAuthorization();

        users.MapPost("/", SendEmailAsync)
            .AddEndpointFilter<ValidationFilter<SendToUserRequest>>();

        users.MapPost("/schedule", ScheduleEmailAsync)
            .AddEndpointFilter<ValidationFilter<ScheduleToUserRequest>>();

        return app;
    }

    private static async Task<Results<Accepted<EmailQueueReply>, NotFound, BadRequest<string>>> SendEmailAsync(
        [AsParameters] SendToUserRequest request,
        IPeopleApiClient people,
        IEmailMessageRepository repository,
        HttpRequest httpRequest,
        CancellationToken ct
    )
    {
        var account = await people.GetAccountAsync(request.UserId, ct);
        if (account is null)
            return TypedResults.NotFound();

        return await QueueEmailAsync(account.Email, request.Subject, null, httpRequest, repository, ct);
    }

    private static async Task<Results<Accepted<EmailQueueReply>, NotFound, BadRequest<string>>> ScheduleEmailAsync(
        [AsParameters] ScheduleToUserRequest request,
        IPeopleApiClient people,
        IEmailMessageRepository repository,
        HttpRequest httpRequest,
        CancellationToken ct
    )
    {
        var account = await people.GetAccountAsync(request.UserId, ct);
        if (account is null)
            return TypedResults.NotFound();

        return await QueueEmailAsync(account.Email, request.Subject, account.Timezone, httpRequest, repository, ct);
    }

    private static async Task<Results<Accepted<EmailQueueReply>, NotFound, BadRequest<string>>> QueueEmailAsync(
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
