using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("admins");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        builder.Property(a => a.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        builder.Property(a => a.CompanyName).HasColumnName("company_name").HasMaxLength(255).IsRequired();
        builder.Property(a => a.PlanId).HasColumnName("plan_id");
        builder.Property(a => a.Role).HasColumnName("role").HasMaxLength(20)
            .HasConversion(
                v => v.ToString().ToLower(),
                v => Enum.Parse<AdminRole>(v, true))
            .HasDefaultValue(AdminRole.Admin);
        builder.Property(a => a.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
        builder.Property(a => a.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");

        // Refresh tokens
        builder.Property(a => a.RefreshToken).HasColumnName("refresh_token").HasMaxLength(128);
        builder.Property(a => a.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");

        // Email verification
        builder.Property(a => a.EmailVerificationToken).HasColumnName("email_verification_token").HasMaxLength(128);
        builder.Property(a => a.EmailVerificationExpiresAt).HasColumnName("email_verification_expires_at");

        // Password reset
        builder.Property(a => a.PasswordResetToken).HasColumnName("password_reset_token").HasMaxLength(128);
        builder.Property(a => a.PasswordResetExpiresAt).HasColumnName("password_reset_expires_at");

        builder.HasIndex(a => a.Email).IsUnique();

        builder.HasOne(a => a.Plan)
            .WithMany(p => p.Admins)
            .HasForeignKey(a => a.PlanId);

        builder.ToTable(t => t.HasCheckConstraint("chk_admin_role", "role IN ('admin', 'superadmin')"));
    }
}
