using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class SignatureConfiguration : IEntityTypeConfiguration<Signature>
{
    public void Configure(EntityTypeBuilder<Signature> builder)
    {
        builder.ToTable("signatures");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(s => s.DocumentId).HasColumnName("document_id");
        builder.Property(s => s.EmployeeId).HasColumnName("employee_id");
        builder.Property(s => s.PhotoFileKey).HasColumnName("photo_file_key").HasMaxLength(500).IsRequired();
        builder.Property(s => s.PhotoHash).HasColumnName("photo_hash").HasMaxLength(64).IsRequired();
        builder.Property(s => s.PhotoMimeType).HasColumnName("photo_mime_type").HasMaxLength(50).IsRequired();
        builder.Property(s => s.SignerIp).HasColumnName("signer_ip").HasMaxLength(45).IsRequired();
        builder.Property(s => s.SignerUserAgent).HasColumnName("signer_user_agent").IsRequired();
        builder.Property(s => s.SignerGeolocation).HasColumnName("signer_geolocation").HasColumnType("jsonb");
        builder.Property(s => s.SignerDeviceInfo).HasColumnName("signer_device_info").HasColumnType("jsonb");
        builder.Property(s => s.SignedAt).HasColumnName("signed_at").HasDefaultValueSql("NOW()");
        builder.Property(s => s.ConsentGiven).HasColumnName("consent_given").HasDefaultValue(true);
        builder.Property(s => s.ConsentText).HasColumnName("consent_text").IsRequired();
        builder.Property(s => s.VerificationMethod).HasColumnName("verification_method").HasMaxLength(10)
            .HasConversion(
                v => v == null ? null : v.ToString()!.ToLower(),
                v => v == null ? null : Enum.Parse<VerificationMethod>(v, true));
        builder.Property(s => s.VerificationHash).HasColumnName("verification_hash").HasMaxLength(64).IsFixedLength();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(s => s.DocumentId).IsUnique();

        builder.HasOne(s => s.Document)
            .WithOne(d => d.Signature)
            .HasForeignKey<Signature>(s => s.DocumentId);

        builder.HasOne(s => s.Employee)
            .WithMany(e => e.Signatures)
            .HasForeignKey(s => s.EmployeeId);

        builder.ToTable(t => t.HasCheckConstraint("chk_verification_method", "verification_method IN ('otp', 'cpf', 'dob')"));
    }
}
