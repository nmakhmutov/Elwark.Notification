using MongoDB.Bson.Serialization;
using MongoDB.Driver;
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

        BsonClassMap.RegisterClassMap<Sendgrid>();
        BsonClassMap.RegisterClassMap<Gmail>();
    }

    public NotificationDbContext(MongoDbOptions settings)
    {
        var mongoSettings = MongoClientSettings.FromUrl(new MongoUrl(settings.ConnectionString));

        // mongoSettings.ClusterConfigurator = builder =>
        //     builder.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(e =>
        //         System.Console.WriteLine(MongoDB.Bson.BsonExtensionMethods.ToJson(e.Command,
        //             new MongoDB.Bson.IO.JsonWriterSettings {Indent = true})));

        var client = new MongoClient(mongoSettings);
        Database = client.GetDatabase(settings.Database);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<EmailProvider> EmailProviders =>
        Database.GetCollection<EmailProvider>("email_providers");

    public IMongoCollection<PostponedEmail> PostponedEmails =>
        Database.GetCollection<PostponedEmail>("postponed_emails");
}
