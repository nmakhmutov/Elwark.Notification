using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Notification.Api.Infrastructure.BsonSerializers;
using Notification.Api.Models;

namespace Notification.Api.Infrastructure;

public sealed class NotificationDbContext
{
    static NotificationDbContext()
    {
        BsonClassMap.RegisterClassMap<EmailProvider>(map =>
        {
            map.AutoMap();
            map.MapIdProperty(x => x.Id);
        });
        
        BsonSerializer.RegisterSerializer(new DateOnlySerializer());

        BsonClassMap.RegisterClassMap<Sendgrid>();
        BsonClassMap.RegisterClassMap<Gmail>();
    }

    public NotificationDbContext(MongoUrl url)
    {
        ArgumentNullException.ThrowIfNull(url, nameof(url));
        ArgumentException.ThrowIfNullOrEmpty(url.DatabaseName, nameof(url.DatabaseName));

        var settings = MongoClientSettings.FromUrl(url);
        settings.LinqProvider = LinqProvider.V2;
        settings.MinConnectionPoolSize = 1;
        settings.MaxConnectionPoolSize = 30;

        // settings.ClusterConfigurator = builder =>
        // builder.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(e =>
        // Console.WriteLine(MongoDB.Bson.BsonExtensionMethods.ToJson(e.Command,
        // new MongoDB.Bson.IO.JsonWriterSettings { Indent = true })));

        var client = new MongoClient(settings);
        Database = client.GetDatabase(url.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<EmailProvider> EmailProviders =>
        Database.GetCollection<EmailProvider>("email_providers");

    public IMongoCollection<PostponedEmail> PostponedEmails =>
        Database.GetCollection<PostponedEmail>("postponed_emails");
}
