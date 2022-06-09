// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

using MongoDB.Bson;

namespace Notification.Api.Models;

public sealed class PostponedEmail
{
    public PostponedEmail(string email, string subject, string body, DateTime sendAt, string timeZone)
    {
        Id = ObjectId.GenerateNewId();
        Email = email;
        Subject = subject;
        Body = body;
        SendAt = sendAt;
        TimeZone = timeZone;
    }

    public ObjectId Id { get; private set; }

    public string Email { get; private set; }

    public string Subject { get; private set; }

    public string Body { get; private set; }

    public DateTime SendAt { get; private set; }

    public string TimeZone { get; private set; }
}
