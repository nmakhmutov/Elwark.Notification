using System.Net.Http.Headers;
using Notification.Api.Grpc;
using Notification.Api.Infrastructure;
using Notification.Api.Infrastructure.Provider;
using Notification.Api.Infrastructure.Provider.Gmail;
using Notification.Api.Infrastructure.Provider.SendGrid;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.IntegrationEvents.EventHandling;
using Notification.Api.IntegrationEvents.Events;
using Notification.Api.Job;
using Grpc.AspNetCore.FluentValidation;
using Grpc.AspNetCore.Server;
using MongoDB.Driver;
using Quartz;
using Serilog;

const string appName = "Notification.Api";
const string topic = "notification.emails.created";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCorrelationId(options => options.UpdateTraceIdentifier = true);

builder.Services
    .AddSingleton(_ => new NotificationDbContext(new MongoUrl(builder.Configuration.GetConnectionString("Mongodb")!)))
    .AddSingleton<IEmailProviderRepository, EmailProviderRepository>();

var assemblies = AppDomain.CurrentDomain.GetAssemblies();

builder.Services
    .AddValidatorsFromAssemblies(assemblies);

builder.Services
    .AddKafka(builder.Configuration.GetConnectionString("Kafka")!)
    .AddProducer<EmailCreatedIntegrationEvent>(producer => producer.WithTopic(topic))
    .AddConsumer<EmailCreatedIntegrationEvent, EmailMessageCreatedHandler>(consumer =>
        consumer.WithTopic(topic)
            .WithGroupId(appName)
            .WithWorkers(4)
            .CreateTopicIfNotExists(4)
    );

builder.Services
    .AddTransient<IEmailSender>(_ =>
        new GmailProvider(builder.Configuration["Gmail:UserName"]!, builder.Configuration["Gmail:Password"]!)
    );

builder.Services
    .AddHttpClient<IEmailSender, SendgridProvider>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Sendgrid:Host"]!);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", builder.Configuration["Sendgrid:Key"]);
    });

builder.Services
    .AddQuartz(configurator =>
    {
        configurator.UseMicrosoftDependencyInjectionJobFactory();

        configurator.ScheduleJob<UpdateProviderJob>(trigger => trigger
            .WithIdentity(nameof(UpdateProviderJob))
            .StartAt(DateBuilder.EvenHourDate(DateTimeOffset.UtcNow))
            .WithSimpleSchedule(scheduleBuilder => scheduleBuilder.WithIntervalInHours(1).RepeatForever())
        );

        configurator.ScheduleJob<PostponedEmailJob>(trigger => trigger
            .WithIdentity(nameof(PostponedEmailJob))
            .StartAt(DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow))
            .WithSimpleSchedule(scheduleBuilder => scheduleBuilder.WithIntervalInMinutes(1).RepeatForever())
        );
    })
    .AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddGrpc(options =>
{
    options.UseCorrelationId();
    options.EnableMessageValidation();
});

builder.Host
    .UseSerilog((context, configuration) => configuration
        .Enrich.WithProperty("ApplicationName", appName)
        .ReadFrom.Configuration(context.Configuration)
    );

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

    await new NotificationDbContextSeed(context)
        .SeedAsync();
}

app.MapGrpcService<NotificationService>();

app.Run();
