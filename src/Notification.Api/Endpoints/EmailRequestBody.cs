namespace Notification.Api.Endpoints;

internal static class EmailRequestBody
{
    private const string TextType = "text/plain";
    private const string HtmlType = "text/html";
    private const string UnsupportedMessage = $"Supported content types are {TextType} and {HtmlType}";
    private const string RequiredMessage = "Request body is required.";

    public static RouteHandlerBuilder AcceptsEmailBody(this RouteHandlerBuilder builder) =>
        builder.Accepts<string>(TextType, HtmlType);

    public static async Task<ReadResult> ReadAsync(HttpRequest request, CancellationToken ct)
    {
        var isHtml = TryGetIsHtml(request.ContentType);
        if (!isHtml.HasValue)
            return new ReadResult.Fail(UnsupportedMessage);

        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
            return new ReadResult.Fail(RequiredMessage);

        return new ReadResult.Success(body, isHtml.Value);
    }

    private static bool? TryGetIsHtml(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return null;

        var mediaType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        if (mediaType.Equals(HtmlType, StringComparison.OrdinalIgnoreCase))
            return true;

        if (mediaType.Equals(TextType, StringComparison.OrdinalIgnoreCase))
            return false;

        return null;
    }

    public abstract record ReadResult
    {
        public sealed record Success(string Body, bool IsHtml) : ReadResult;

        public sealed record Fail(string Error) : ReadResult;
    }
}
