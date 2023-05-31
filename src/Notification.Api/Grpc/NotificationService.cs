using FluentValidation;
using Grpc.Core;
using MongoDB.Driver;
using Notification.Api.Infrastructure;
using Notification.Api.Integration;
using Notification.Api.IntegrationEvents.Events;
using Notification.Api.Models;
using Notification.Grpc;

namespace Notification.Api.Grpc;

internal sealed class NotificationService : global::Notification.Grpc.NotificationService.NotificationServiceBase
{
    private readonly IIntegrationEventBus _bus;
    private readonly NotificationDbContext _dbContext;

    public NotificationService(IIntegrationEventBus bus, NotificationDbContext dbContext)
    {
        _bus = bus;
        _dbContext = dbContext;
    }

    public override async Task<EmailReply> SendEmail(SendRequest request, ServerCallContext context)
    {
        var evt = new EmailCreatedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            request.Email,
            request.Subject,
            request.Body
        );

        await _bus.PublishAsync(evt, context.CancellationToken);

        return new EmailReply { Status = EmailReply.Types.Status.Sent };
    }

    public override async Task<EmailReply> ScheduleEmail(ScheduleRequest request, ServerCallContext context)
    {
        if (CalcDelay(ParseTimeZone(request.TimeZone)) is { } delay)
        {
            var email = new PostponedEmail(request.Email, request.Subject, request.Body, delay);

            await _dbContext.PostponedEmails
                .InsertOneAsync(email, new InsertOneOptions(), context.CancellationToken);

            return new EmailReply { Status = EmailReply.Types.Status.Postponed };
        }

        var evt = new EmailCreatedIntegrationEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            request.Email,
            request.Subject,
            request.Body
        );

        await _bus.PublishAsync(evt, context.CancellationToken);

        return new EmailReply { Status = EmailReply.Types.Status.Sent };
    }

    private static DateTime? CalcDelay(TimeZoneInfo timezone)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
        if (local.Hour is >= 9 and < 21)
            return null;

        var date = local.Hour < 9 ? local : local.AddDays(1);

        return TimeZoneInfo.ConvertTimeToUtc(new DateTime(date.Year, date.Month, date.Day, 9, 0, 0), timezone);
    }

    private static TimeZoneInfo ParseTimeZone(string timeZone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}

public sealed class SendRequestValidator : AbstractValidator<SendRequest>
{
    public SendRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Subject)
            .NotEmpty();

        RuleFor(x => x.Body)
            .NotEmpty();
    }
}

public sealed class ScheduleRequestValidator : AbstractValidator<ScheduleRequest>
{
    public ScheduleRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Subject)
            .NotEmpty();

        RuleFor(x => x.Body)
            .NotEmpty();

        RuleFor(x => x.TimeZone)
            .NotEmpty();
    }
}
