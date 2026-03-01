using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class SigningVerificationConfiguration : IEntityTypeConfiguration<SigningVerification>
{
    public void Configure(EntityTypeBuilder<SigningVerification> builder)
    {
        builder.ToTable("signing_verifications");

        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(v => v.DocumentId).HasColumnName("document_id");
        builder.Property(v => v.EmployeeId).HasColumnName("employee_id");
        builder.Property(v => v.Method).HasColumnName("method").HasMaxLength(10)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<VerificationMethod>(v, true));
        builder.Property(v => v.Verified).HasColumnName("verified").HasDefaultValue(false);
        builder.Property(v => v.VerifiedAt).HasColumnName("verified_at");
        builder.Property(v => v.OtpHash).HasColumnName("otp_hash").HasMaxLength(64).IsFixedLength();
        builder.Property(v => v.OtpExpiresAt).HasColumnName("otp_expires_at");
        builder.Property(v => v.LastSentAt).HasColumnName("last_sent_at");
        builder.Property(v => v.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(v => v.AttemptWindowStart).HasColumnName("attempt_window_start");
        builder.Property(v => v.ExpiresAt).HasColumnName("expires_at");
        builder.Property(v => v.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(v => v.DocumentId).HasDatabaseName("idx_signing_verifications_document");

        // Only one active (unverified) verification per document
        builder.HasIndex(v => v.DocumentId)
            .HasFilter("verified = false")
            .IsUnique()
            .HasDatabaseName("idx_signing_verifications_active");

        builder.HasOne(v => v.Document)
            .WithOne(d => d.SigningVerification)
            .HasForeignKey<SigningVerification>(v => v.DocumentId);

        builder.HasOne(v => v.Employee)
            .WithMany()
            .HasForeignKey(v => v.EmployeeId);

        builder.ToTable(t => t.HasCheckConstraint("chk_verification_method", "method IN ('otp', 'cpf', 'dob')"));
    }
}
