using System.Diagnostics;
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
            .AcceptsEmailBody()
            .AddEndpointFilter<ValidationFilter<SendToUserRequest>>();

        users.MapPost("/schedule", ScheduleEmailAsync)
            .AcceptsEmailBody()
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
        return await EmailRequestBody.ReadAsync(request, ct) switch
        {
            EmailRequestBody.ReadResult.Fail x => TypedResults.BadRequest(x.Error),
            EmailRequestBody.ReadResult.Success x => await Success(x),
            _ => throw new UnreachableException()
        };

        async Task<Accepted<EmailQueueReply>> Success(EmailRequestBody.ReadResult.Success success)
        {
            var now = DateTime.UtcNow;
            var sendAt = EmailScheduleCalculator.CalculateSendAt(now, timezone);

            var message = EmailMessage.Create(email, subject, success.Body, success.IsHtml, sendAt);
            await repository.CreateAsync(message, ct);

            var status = sendAt <= now ? "queued" : "postponed";
            return TypedResults.Accepted(string.Empty, new EmailQueueReply(status, message.SendAt));
        }
    }
}
