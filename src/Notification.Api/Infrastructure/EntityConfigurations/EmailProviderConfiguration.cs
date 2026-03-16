using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Api.Models;

namespace Notification.Api.Infrastructure.EntityConfigurations;

internal sealed class EmailProviderConfiguration : IEntityTypeConfiguration<EmailProvider>
{
    public void Configure(EntityTypeBuilder<EmailProvider> builder)
    {
        builder.ToTable("email_providers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Limit)
            .HasColumnName("limit")
            .IsRequired();

        builder.Property(x => x.Balance)
            .HasColumnName("balance")
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(x => x.ResetAt)
            .HasColumnName("reset_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.Version)
            .IsRowVersion();

        builder.HasDiscriminator(x => x.Id)
            .HasValue<Sendgrid>(EmailProvider.Type.Sendgrid)
            .HasValue<Models.Resend>(EmailProvider.Type.Resend);
    }
}
