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
using SendGrid;
using Serilog;
using Scalar.AspNetCore;

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
    .AddScoped<IEmailProviderRepository, EmailProviderRepository>()
    .AddScoped<IEmailMessageRepository, EmailMessageRepository>();

builder.Services
    .AddOpenApi()
    .AddValidatorsFromAssemblies(assemblies);

builder.Services
    .AddTransient<IEmailSender, SendgridProvider>(provider =>
    {
        var client = new SendGridClient(builder.Configuration["Sendgrid:Key"]);
        return new SendgridProvider(client, provider.GetRequiredService<ILogger<SendgridProvider>>());
    });

builder.Services
    .AddTransient<IEmailSender, ResendProvider>()
    .Configure<ResendClientOptions>(x => x.ApiToken = builder.Configuration["Resend:Key"]!)
    .AddHttpClient<IResend, ResendClient>();

builder.Services
    .AddClientCredentialsTokenManagement()
    .AddClient(ClientCredentialsClientName.Parse("people"), client =>
    {
        client.TokenEndpoint = new Uri(builder.Configuration.GetRequiredUri("Authentication:Authority"), "connect/token");
        client.ClientId = ClientId.Parse(builder.Configuration.GetRequiredString("People:ClientId"));
        client.ClientSecret = ClientSecret.Parse(builder.Configuration.GetRequiredString("People:ClientSecret"));
        client.Scope = Scope.Parse(builder.Configuration.GetRequiredString("People:Scope"));
    });

builder.Services
    .AddHttpClient<IPeopleApiClient, PeopleApiClient>(client =>
        client.BaseAddress = builder.Configuration.GetRequiredUri("People:Host")
    )
    .AddClientCredentialsTokenHandler(ClientCredentialsClientName.Parse("people"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration.GetRequiredUri("Authentication:Authority").AbsoluteUri;
        options.Audience = builder.Configuration.GetRequiredString("Authentication:Audience");
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
    .AddDefaultPolicy("Default", policy => policy.RequireClaim("scope", "elwark.notification.api"));

builder.Services
    .AddQuartz(configurator =>
    {
        configurator.ScheduleJob<SendEmailJob>(trigger => trigger
            .WithIdentity(nameof(SendEmailJob))
            .StartAt(DateBuilder.NextGivenSecondDate(DateTimeOffset.UtcNow, 0))
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInSeconds(5)
                .RepeatForever()
            )
        );

        configurator.ScheduleJob<UpdateProviderBalanceJob>(trigger => trigger
            .WithIdentity(nameof(UpdateProviderBalanceJob))
            .StartAt(DateBuilder.NextGivenMinuteDate(DateTimeOffset.UtcNow, 0))
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInHours(1)
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

    await new NotificationDbContextSeed(context)
        .SeedAsync();
}

app.UseAuthentication()
    .UseAuthorization();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs");
}

app.MapEmailEndpoints();
app.MapUserEndpoints();

await app.RunAsync();
