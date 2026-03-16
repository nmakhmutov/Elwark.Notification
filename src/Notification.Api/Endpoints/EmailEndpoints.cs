using System.Diagnostics;
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
            .AcceptsEmailBody()
            .AddEndpointFilter<ValidationFilter<SendToAddressRequest>>();

        emails.MapPost("/schedule", ScheduleEmailAsync)
            .AcceptsEmailBody()
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
