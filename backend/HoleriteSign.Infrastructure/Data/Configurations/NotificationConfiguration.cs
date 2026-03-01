using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(n => n.DocumentId).HasColumnName("document_id");
        builder.Property(n => n.EmployeeId).HasColumnName("employee_id");
        builder.Property(n => n.Channel).HasColumnName("channel").HasMaxLength(20)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<NotificationChannel>(v, true));
        builder.Property(n => n.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<NotificationStatus>(v, true))
            .HasDefaultValue(NotificationStatus.Pending);
        builder.Property(n => n.ExternalId).HasColumnName("external_id").HasMaxLength(255);
        builder.Property(n => n.ErrorMessage).HasColumnName("error_message");
        builder.Property(n => n.SentAt).HasColumnName("sent_at");
        builder.Property(n => n.DeliveredAt).HasColumnName("delivered_at");
        builder.Property(n => n.ReadAt).HasColumnName("read_at");
        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(n => n.DocumentId).HasDatabaseName("idx_notifications_document");

        builder.HasOne(n => n.Document)
            .WithMany(d => d.Notifications)
            .HasForeignKey(n => n.DocumentId);

        builder.HasOne(n => n.Employee)
            .WithMany()
            .HasForeignKey(n => n.EmployeeId);
    }
}
