using Notification.Api.Models;

namespace Notification.Api.Tests.Models;

public sealed class EmailMessageTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldReturn_PendingMessage_WithCorrectValues()
    {
        var sendAt = DateTime.UtcNow.AddMinutes(5);

        var message = EmailMessage.Create("test@example.com", "Hello", "Body", false, sendAt);

        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(EmailMessage.QueueStatus.Pending, message.Status);
        Assert.Equal("test@example.com", message.Email);
        Assert.Equal("Hello", message.Subject);
        Assert.Equal("Body", message.Body);
        Assert.False(message.IsHtml);
        Assert.Equal(0, message.Attempts);
        Assert.Null(message.Error);
        Assert.Equal(sendAt, message.SendAt);
    }

    [Fact]
    public void Create_WithHtmlBody_ShouldSetIsHtmlTrue()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "<h1>Body</h1>", true, DateTime.UtcNow);

        Assert.True(message.IsHtml);
    }

    [Fact]
    public void Create_WithPastSendAt_ShouldStillBePending()
    {
        var pastSendAt = DateTime.UtcNow.AddDays(-1);

        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, pastSendAt);

        Assert.Equal(EmailMessage.QueueStatus.Pending, message.Status);
        Assert.Equal(pastSendAt, message.SendAt);
    }

    [Fact]
    public void Create_EachCallShouldProduceUniqueId()
    {
        var a = EmailMessage.Create("a@example.com", "Subject", "Body", false, DateTime.UtcNow);
        var b = EmailMessage.Create("b@example.com", "Subject", "Body", false, DateTime.UtcNow);

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Create_CreatedAtShouldBeRecentUtcTime()
    {
        var before = DateTime.UtcNow;

        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);

        Assert.True(message.CreatedAt >= before);
        Assert.True(message.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_AttemptsAndErrorShouldBeZeroAndNull()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);

        Assert.Equal(0, message.Attempts);
        Assert.Null(message.Error);
    }

    // ── MarkProcessing ────────────────────────────────────────────────────────

    [Fact]
    public void MarkProcessing_ShouldTransitionToProcessing_AndUpdateTimestamp()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);
        var now = DateTime.UtcNow.AddSeconds(1);

        message.MarkProcessing(now);

        Assert.Equal(EmailMessage.QueueStatus.Processing, message.Status);
        Assert.Equal(now, message.UpdatedAt);
    }

    [Fact]
    public void MarkProcessing_CalledTwice_ShouldUpdateTimestampEachTime()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);
        var first = DateTime.UtcNow.AddSeconds(1);
        var second = DateTime.UtcNow.AddSeconds(2);

        message.MarkProcessing(first);
        message.MarkProcessing(second);

        Assert.Equal(EmailMessage.QueueStatus.Processing, message.Status);
        Assert.Equal(second, message.UpdatedAt);
    }

    [Fact]
    public void MarkProcessing_ShouldNotChangeAttemptsOrError()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);

        message.MarkProcessing(DateTime.UtcNow);

        Assert.Equal(0, message.Attempts);
        Assert.Null(message.Error);
    }

    // ── MarkCompleted ─────────────────────────────────────────────────────────

    [Fact]
    public void MarkCompleted_ShouldTransitionToCompleted_AndIncrementAttempts_AndClearError()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);
        message.Reschedule(DateTime.UtcNow.AddMinutes(1), "previous error");

        message.MarkCompleted();

        Assert.Equal(EmailMessage.QueueStatus.Completed, message.Status);
        Assert.Equal(2, message.Attempts);
        Assert.Null(message.Error);
    }

    [Fact]
    public void MarkCompleted_OnFreshMessage_ShouldHaveOneAttempt()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);

        message.MarkCompleted();

        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public void MarkCompleted_CalledTwice_ShouldIncrementAttemptsTwice()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);

        message.MarkCompleted();
        message.MarkCompleted();

        Assert.Equal(2, message.Attempts);
    }

    [Fact]
    public void MarkCompleted_ShouldAlwaysClearError_EvenIfSetMultipleTimes()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);
        message.Reschedule(DateTime.UtcNow.AddMinutes(1), "error 1");
        message.Reschedule(DateTime.UtcNow.AddMinutes(2), "error 2");

        message.MarkCompleted();

        Assert.Null(message.Error);
    }

    // ── Reschedule ────────────────────────────────────────────────────────────

    [Fact]
    public void Reschedule_ShouldMoveToPending_WithNewSendAt_AndError_AndIncrementAttempts()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);
        var newSendAt = DateTime.UtcNow.AddMinutes(5);

        message.Reschedule(newSendAt, "some error");

        Assert.Equal(EmailMessage.QueueStatus.Pending, message.Status);
        Assert.Equal(newSendAt, message.SendAt);
        Assert.Equal("some error", message.Error);
        Assert.Equal(1, message.Attempts);
    }

    [Fact]
    public void Reschedule_AfterMarkProcessing_ShouldReturnToPending()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);
        message.MarkProcessing(DateTime.UtcNow);

        message.Reschedule(DateTime.UtcNow.AddMinutes(1), "send failed");

        Assert.Equal(EmailMessage.QueueStatus.Pending, message.Status);
    }

    [Fact]
    public void Reschedule_WithNullError_ShouldClearPreviousError()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);
        message.Reschedule(DateTime.UtcNow.AddMinutes(1), "initial error");

        message.Reschedule(DateTime.UtcNow.AddMinutes(2), null);

        Assert.Null(message.Error);
    }

    [Fact]
    public void Reschedule_ShouldOverwritePreviousError()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);
        message.Reschedule(DateTime.UtcNow.AddMinutes(1), "first error");

        message.Reschedule(DateTime.UtcNow.AddMinutes(2), "second error");

        Assert.Equal("second error", message.Error);
    }

    [Fact]
    public void Reschedule_MultipleTimes_ShouldAccumulateAttempts()
    {
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, DateTime.UtcNow);

        message.Reschedule(DateTime.UtcNow.AddMinutes(1), "error 1");
        message.Reschedule(DateTime.UtcNow.AddMinutes(2), "error 2");
        message.Reschedule(DateTime.UtcNow.AddMinutes(3), "error 3");

        Assert.Equal(3, message.Attempts);
    }

    [Fact]
    public void Reschedule_ShouldUpdateSendAt_ToNewValue()
    {
        var original = DateTime.UtcNow.AddMinutes(1);
        var message = EmailMessage.Create("test@example.com", "Subject", "Body", false, original);
        var rescheduled = DateTime.UtcNow.AddMinutes(10);

        message.Reschedule(rescheduled, null);

        Assert.Equal(rescheduled, message.SendAt);
    }
}
