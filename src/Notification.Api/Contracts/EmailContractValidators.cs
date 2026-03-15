using FluentValidation;

namespace Notification.Api.Contracts;

public sealed class SendToAddressRequestValidator : AbstractValidator<SendToAddressRequest>
{
    public SendToAddressRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Subject)
            .NotEmpty();
    }
}

public sealed class ScheduleToAddressRequestValidator : AbstractValidator<ScheduleToAddressRequest>
{
    public ScheduleToAddressRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Subject)
            .NotEmpty();

        RuleFor(x => x.Timezone)
            .NotEmpty();
    }
}

public sealed class SendToUserRequestValidator : AbstractValidator<SendToUserRequest>
{
    public SendToUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        RuleFor(x => x.Subject)
            .NotEmpty();
    }
}

public sealed class ScheduleToUserRequestValidator : AbstractValidator<ScheduleToUserRequest>
{
    public ScheduleToUserRequestValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        RuleFor(x => x.Subject)
            .NotEmpty();
    }
}
