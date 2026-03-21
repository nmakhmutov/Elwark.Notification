namespace Notification.Api.Models;

public sealed class EmailMessage
{
    public enum QueueStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string Subject { get; private set; }

    public string Body { get; private set; }

    public string? Error { get; private set; }

    public bool IsHtml { get; private set; }

    public QueueStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime SendAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private EmailMessage()
    {
        Email = string.Empty;
        Subject = string.Empty;
        Body = string.Empty;
    }

    public EmailMessage(
        Guid id,
        QueueStatus status,
        string email,
        string subject,
        string body,
        bool isHtml,
        int attempts,
        string? error,
        DateTime sendAt,
        DateTime updatedAt,
        DateTime createdAt
    )
    {
        Id = id;
        Status = status;
        Email = email;
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
        Attempts = attempts;
        Error = error;
        SendAt = sendAt;
        UpdatedAt = updatedAt;
        CreatedAt = createdAt;
    }

    public static EmailMessage Create(string email, string subject, string body, bool isHtml, DateTime sendAt)
    {
        var now = DateTime.UtcNow;
        var id = Guid.CreateVersion7();

        return new EmailMessage(id, QueueStatus.Pending, email, subject, body, isHtml, 0, null, sendAt, now, now);
    }

    public void MarkProcessing(DateTime now)
    {
        Status = QueueStatus.Processing;
        UpdatedAt = now;
    }

    public void MarkCompleted()
    {
        Status = QueueStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        Error = null;
        Attempts++;
    }

    public void Reschedule(DateTime sendAt, string? error)
    {
        Status = QueueStatus.Pending;
        SendAt = sendAt;
        Error = error;
        UpdatedAt = DateTime.UtcNow;
        Attempts++;
    }
}
