using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(a => a.AdminId).HasColumnName("admin_id");
        builder.Property(a => a.EmployeeId).HasColumnName("employee_id");
        builder.Property(a => a.DocumentId).HasColumnName("document_id");
        builder.Property(a => a.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        builder.Property(a => a.EventData).HasColumnName("event_data").HasColumnType("jsonb");
        builder.Property(a => a.ActorType).HasColumnName("actor_type").HasMaxLength(20)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<ActorType>(v, true));
        builder.Property(a => a.ActorIp).HasColumnName("actor_ip").HasMaxLength(45);
        builder.Property(a => a.ActorUserAgent).HasColumnName("actor_user_agent");
        builder.Property(a => a.PrevHash).HasColumnName("prev_hash").HasMaxLength(64).IsFixedLength();
        builder.Property(a => a.EntryHash).HasColumnName("entry_hash").HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(a => a.ChainVersion).HasColumnName("chain_version").HasDefaultValue(1);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(a => a.DocumentId).HasDatabaseName("idx_audit_document");
        builder.HasIndex(a => a.EmployeeId).HasDatabaseName("idx_audit_employee");
        builder.HasIndex(a => a.EventType).HasDatabaseName("idx_audit_event");
        builder.HasIndex(a => a.CreatedAt).HasDatabaseName("idx_audit_created");

        builder.ToTable(t => t.HasCheckConstraint("chk_actor_type", "actor_type IN ('admin', 'employee', 'system')"));
    }
}
