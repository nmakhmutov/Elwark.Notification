using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Api.Models;

namespace Notification.Api.Infrastructure.EntityConfigurations;

internal sealed class EmailMessageConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("email_messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Subject)
            .HasColumnName("subject")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Body)
            .HasColumnName("body")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.IsHtml)
            .HasColumnName("is_html")
            .IsRequired();

        builder.Property(x => x.SendAt)
            .HasColumnName("send_at")
            .IsRequired();

        builder.Property(x => x.Error)
            .HasColumnName("error")
            .HasMaxLength(1024);

        builder.Property(x => x.Attempts)
            .HasColumnName("attempts");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        
        builder.HasIndex(x => new { x.Status, x.SendAt });
        builder.HasIndex(x => new { x.Status, x.UpdatedAt });
    }
}
