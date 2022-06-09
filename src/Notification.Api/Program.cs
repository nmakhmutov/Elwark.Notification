using System.Net.Http.Headers;
using CorrelationId;
using CorrelationId.DependencyInjection;
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
using Quartz;
using Serilog;

const string appName = "Notification.Api";
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCorrelationId(options =>
    {
        options.UpdateTraceIdentifier = true;
        options.AddToLoggingScope = true;
    })
    .WithTraceIdentifierProvider();

builder.Services
    .AddSingleton(_ => new NotificationDbContext(new MongoDbOptions
    {
        ConnectionString = builder.Configuration["Mongodb:ConnectionString"]!,
        Database = builder.Configuration["Mongodb:Database"]!
    }))
    .AddSingleton<IEmailProviderRepository, EmailProviderRepository>();

var assemblies = AppDomain.CurrentDomain.GetAssemblies();

builder.Services
    .AddValidatorsFromAssemblies(assemblies);

const string topic = "notification.emails.created";
builder.Services
    .AddKafkaMessageBus(appName, builder.Configuration["Kafka:Servers"]!)
    .AddProducer<EmailCreatedIntegrationEvent>(config => config.Topic = topic)
    .AddConsumer<EmailCreatedIntegrationEvent, EmailMessageCreatedHandler>(config =>
    {
        config.Topic = topic;
        config.Threads = 4;
    });

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
            .WithIdentity("UpdateProviderJob")
            .StartAt(DateBuilder.EvenHourDate(DateTimeOffset.UtcNow))
            .WithSimpleSchedule(scheduleBuilder => scheduleBuilder.WithIntervalInHours(1).RepeatForever())
        );

        configurator.ScheduleJob<PostponedEmailJob>(trigger => trigger
            .WithIdentity("PostponedEmailJob")
            .StartAt(DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow))
            .WithSimpleSchedule(scheduleBuilder => scheduleBuilder.WithIntervalInMinutes(1).RepeatForever())
        );
    })
    .AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddGrpc(options => options.EnableMessageValidation());

builder.Host
    .UseSerilog((context, configuration) => configuration
        .Enrich.WithProperty("ApplicationName", appName)
        .ReadFrom.Configuration(context.Configuration)
    );

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

    await new NotificationDbContextSeed(context).SeedAsync();
}

app.UseCorrelationId();

app.MapGrpcService<NotificationService>();

app.Run();
