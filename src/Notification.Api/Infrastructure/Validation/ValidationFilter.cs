using FluentValidation;

namespace Notification.Api.Infrastructure.Validation;

internal sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator) =>
        _validator = validator;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var model = context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
            return await next(context);

        var validation = await _validator.ValidateAsync(model, context.HttpContext.RequestAborted);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        return await next(context);
    }
}
