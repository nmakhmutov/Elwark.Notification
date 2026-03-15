using Microsoft.EntityFrameworkCore;
using Notification.Api.Models;

namespace Notification.Api.Infrastructure;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<EmailProvider> EmailProviders =>
        Set<EmailProvider>();

    public DbSet<EmailMessage> TempEmails =>
        Set<EmailMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
}
