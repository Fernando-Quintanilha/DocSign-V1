using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.EmployeeId).HasColumnName("employee_id");
        builder.Property(d => d.PayPeriodId).HasColumnName("pay_period_id");
        builder.Property(d => d.AdminId).HasColumnName("admin_id");
        builder.Property(d => d.OriginalFilename).HasColumnName("original_filename").HasMaxLength(500).IsRequired();
        builder.Property(d => d.OriginalFileKey).HasColumnName("original_file_key").HasMaxLength(500).IsRequired();
        builder.Property(d => d.OriginalFileHash).HasColumnName("original_file_hash").HasMaxLength(64).IsRequired();
        builder.Property(d => d.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(d => d.SignedFileKey).HasColumnName("signed_file_key").HasMaxLength(500);
        builder.Property(d => d.SignedFileHash).HasColumnName("signed_file_hash").HasMaxLength(64);
        builder.Property(d => d.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<DocumentStatus>(v, true))
            .HasDefaultValue(DocumentStatus.Uploaded);
        builder.Property(d => d.SigningTokenHash).HasColumnName("signing_token_hash").HasMaxLength(64).IsFixedLength();
        builder.Property(d => d.TokenExpiresAt).HasColumnName("token_expires_at");
        builder.Property(d => d.TokenUsedAt).HasColumnName("token_used_at");
        builder.Property(d => d.ViewedAt).HasColumnName("viewed_at");
        builder.Property(d => d.LastNotifiedAt).HasColumnName("last_notified_at");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(d => d.EmployeeId).HasDatabaseName("idx_documents_employee");
        builder.HasIndex(d => d.PayPeriodId).HasDatabaseName("idx_documents_period");
        builder.HasIndex(d => d.SigningTokenHash).IsUnique().HasDatabaseName("idx_documents_token");
        builder.HasIndex(d => d.Status).HasDatabaseName("idx_documents_status");
        builder.HasIndex(d => new { d.EmployeeId, d.PayPeriodId }).IsUnique();

        builder.HasOne(d => d.Employee)
            .WithMany(e => e.Documents)
            .HasForeignKey(d => d.EmployeeId);

        builder.HasOne(d => d.PayPeriod)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.PayPeriodId);

        builder.HasOne(d => d.Admin)
            .WithMany(a => a.Documents)
            .HasForeignKey(d => d.AdminId);

        builder.ToTable(t => t.HasCheckConstraint("chk_document_status", "status IN ('uploaded', 'sent', 'signed', 'expired')"));
    }
}
