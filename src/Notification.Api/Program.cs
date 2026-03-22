using DotNetEnv;
using Duende.AccessTokenManagement;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Notification.Api.Endpoints;
using Notification.Api.Infrastructure;
using Notification.Api.Infrastructure.People;
using Notification.Api.Infrastructure.Provider;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.Job;
using Quartz;
using Resend;
using Scalar.AspNetCore;
using SendGrid;
using Serilog;

const string appName = "Notification.Api";

Env.Load();

var builder = WebApplication.CreateBuilder(args);
var assemblies = AppDomain.CurrentDomain.GetAssemblies();

builder.Services
    .AddDbContextFactory<NotificationDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgresql"));

        options.EnableDetailedErrors(builder.Environment.IsDevelopment());
        options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    })
    .AddScoped(sp => sp.GetRequiredService<IDbContextFactory<NotificationDbContext>>().CreateDbContext())
    .AddScoped<IEmailMessageRepository, EmailMessageRepository>();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<NotificationDbContext>();

builder.Services
    .AddOpenApi()
    .AddValidatorsFromAssemblies(assemblies);

builder.Services
    .AddSingleton<IEmailProvider, ResendEmailProvider>()
    .Configure<ResendClientOptions>(x => x.ApiToken = builder.Configuration.GetString("Resend:Key"))
    .AddHttpClient(nameof(ResendClient));

builder.Services
    .AddSingleton<IEmailProvider>(provider =>
    {
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var logger = provider.GetRequiredService<ILogger<SendGridEmailProvider>>();

        return new SendGridEmailProvider(factory,builder.Configuration.GetString("SendGrid:Key"), logger);
    })
    .AddHttpClient(nameof(SendGridClient));

builder.Services
    .AddSingleton<IEmailProvider>(provider => new GmailEmailProvider(
        builder.Configuration.GetString("Gmail:Username"),
        builder.Configuration.GetString("Gmail:Key"),
        provider.GetRequiredService<ILogger<GmailEmailProvider>>()
    ));

builder.Services
    .AddSingleton<IEmailSender, RoundRobinEmailSender>();

builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(ClientCredentialsClientName.Parse("people"), client =>
    {
        client.TokenEndpoint = builder.Configuration.GetUri("Authentication:Authority", "connect/token");
        client.ClientId = ClientId.Parse(builder.Configuration.GetString("People:ClientId"));
        client.ClientSecret = ClientSecret.Parse(builder.Configuration.GetString("People:ClientSecret"));
        client.Scope = Scope.Parse(builder.Configuration.GetString("People:Scope"));
    });

builder.Services
    .AddHttpClient<IPeopleApiClient, PeopleApiClient>(client =>
        client.BaseAddress = builder.Configuration.GetUri("People:Host")
    )
    .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("people"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration.GetUri("Authentication:Authority").AbsoluteUri;
        options.Audience = builder.Configuration.GetString("Authentication:Audience");
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromSeconds(10)
        };
    });

builder.Services
    .AddAuthorizationBuilder()
    .AddDefaultPolicy("Default", policy => policy.RequireClaim("scope"));

builder.Services
    .AddQuartz(configurator =>
    {
        configurator.ScheduleJob<SendEmailJob>(trigger => trigger
            .WithIdentity(nameof(SendEmailJob))
            .StartNow()
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInSeconds(5)
                .RepeatForever()
            )
        );

        configurator.ScheduleJob<DeleteCompletedEmailsJob>(trigger => trigger
            .WithIdentity(nameof(DeleteCompletedEmailsJob))
            .StartAt(DateBuilder.NextGivenMinuteDate(DateTimeOffset.UtcNow, 10))
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInHours(1)
                .RepeatForever()
            )
        );
    })
    .AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Host
    .UseSerilog((context, configuration) => configuration
        .Enrich.WithProperty("ApplicationName", appName)
        .ReadFrom.Configuration(context.Configuration)
    );

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await context.Database.MigrateAsync();
}

app.UseAuthentication()
    .UseAuthorization();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs");
}

app.MapHealthChecks("/health")
    .AllowAnonymous();

app.MapEmailEndpoints();
app.MapUserEndpoints();

await app.RunAsync();
