using System.Text;
using Microsoft.AspNetCore.Http;
using Notification.Api.Endpoints;

namespace Notification.Api.Tests.Endpoints;

public sealed class EmailRequestBodyTests
{
    // ── Supported content types ───────────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_WithTextPlain_ShouldReturnSuccess_AndIsHtmlFalse()
    {
        var request = CreateRequest("text/plain", "Hello world");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        var success = Assert.IsType<EmailRequestBody.ReadResult.Success>(result);
        Assert.False(success.IsHtml);
        Assert.Equal("Hello world", success.Body);
    }

    [Fact]
    public async Task ReadAsync_WithTextHtml_ShouldReturnSuccess_AndIsHtmlTrue()
    {
        var request = CreateRequest("text/html", "<h1>Hello</h1>");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        var success = Assert.IsType<EmailRequestBody.ReadResult.Success>(result);
        Assert.True(success.IsHtml);
        Assert.Equal("<h1>Hello</h1>", success.Body);
    }

    [Fact]
    public async Task ReadAsync_WithTextPlainAndCharset_ShouldReturnSuccess()
    {
        // Parameters after ';' must be stripped before comparing media type
        var request = CreateRequest("text/plain; charset=utf-8", "Hello");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        var success = Assert.IsType<EmailRequestBody.ReadResult.Success>(result);
        Assert.False(success.IsHtml);
    }

    [Fact]
    public async Task ReadAsync_WithTextHtmlAndCharset_ShouldReturnSuccess_AndIsHtmlTrue()
    {
        var request = CreateRequest("text/html; charset=utf-8", "<p>Hi</p>");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        var success = Assert.IsType<EmailRequestBody.ReadResult.Success>(result);
        Assert.True(success.IsHtml);
    }

    [Theory]
    [InlineData("TEXT/PLAIN")]
    [InlineData("Text/Plain")]
    [InlineData("text/PLAIN")]
    public async Task ReadAsync_ContentType_IsCaseInsensitive_ForTextPlain(string contentType)
    {
        var request = CreateRequest(contentType, "Hello");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        Assert.IsType<EmailRequestBody.ReadResult.Success>(result);
    }

    [Theory]
    [InlineData("TEXT/HTML")]
    [InlineData("Text/Html")]
    [InlineData("text/HTML")]
    public async Task ReadAsync_ContentType_IsCaseInsensitive_ForTextHtml(string contentType)
    {
        var request = CreateRequest(contentType, "<b>Hi</b>");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        Assert.IsType<EmailRequestBody.ReadResult.Success>(result);
    }

    [Fact]
    public async Task ReadAsync_ShouldPreserveBodyContentExactly()
    {
        const string body = "Line 1\nLine 2\n\tIndented";
        var request = CreateRequest("text/plain", body);

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        var success = Assert.IsType<EmailRequestBody.ReadResult.Success>(result);
        Assert.Equal(body, success.Body);
    }

    // ── Unsupported / missing content types ───────────────────────────────────

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    [InlineData("multipart/form-data")]
    [InlineData("text/csv")]
    public async Task ReadAsync_WithUnsupportedContentType_ShouldReturnFail(string contentType)
    {
        var request = CreateRequest(contentType, "some body");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        Assert.IsType<EmailRequestBody.ReadResult.Fail>(result);
    }

    [Fact]
    public async Task ReadAsync_WithNullContentType_ShouldReturnFail()
    {
        var request = CreateRequest(null, "some body");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        Assert.IsType<EmailRequestBody.ReadResult.Fail>(result);
    }

    [Fact]
    public async Task ReadAsync_WithEmptyContentType_ShouldReturnFail()
    {
        var request = CreateRequest("", "some body");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        Assert.IsType<EmailRequestBody.ReadResult.Fail>(result);
    }

    // ── Empty / whitespace body ───────────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_WithEmptyBody_ShouldReturnFail()
    {
        var request = CreateRequest("text/plain", "");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        Assert.IsType<EmailRequestBody.ReadResult.Fail>(result);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("   \t  \n  ")]
    public async Task ReadAsync_WithWhitespaceOnlyBody_ShouldReturnFail(string body)
    {
        var request = CreateRequest("text/plain", body);

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        Assert.IsType<EmailRequestBody.ReadResult.Fail>(result);
    }

    // ── Error messages ────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadAsync_WithUnsupportedContentType_FailMessageMentionsSupportedTypes()
    {
        var request = CreateRequest("application/json", "body");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        var fail = Assert.IsType<EmailRequestBody.ReadResult.Fail>(result);
        Assert.Contains("text/plain", fail.Error);
        Assert.Contains("text/html", fail.Error);
    }

    [Fact]
    public async Task ReadAsync_WithEmptyBody_FailMessageMentionsRequired()
    {
        var request = CreateRequest("text/plain", "");

        var result = await EmailRequestBody.ReadAsync(request, CancellationToken.None);

        var fail = Assert.IsType<EmailRequestBody.ReadResult.Fail>(result);
        Assert.NotEmpty(fail.Error);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HttpRequest CreateRequest(string? contentType, string body)
    {
        var context = new DefaultHttpContext();
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        context.Request.ContentType = contentType;
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;
        return context.Request;
    }
}
