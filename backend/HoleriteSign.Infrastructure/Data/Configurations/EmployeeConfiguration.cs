using HoleriteSign.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.AdminId).HasColumnName("admin_id");
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
        builder.Property(e => e.WhatsApp).HasColumnName("whatsapp").HasMaxLength(20);
        builder.Property(e => e.CpfEncrypted).HasColumnName("cpf_encrypted");
        builder.Property(e => e.CpfLast4).HasColumnName("cpf_last4").HasMaxLength(4).IsFixedLength();
        builder.Property(e => e.BirthDateEncrypted).HasColumnName("birth_date_encrypted");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.AdminId).HasDatabaseName("idx_employees_admin");

        // At least email or WhatsApp must be present
        builder.ToTable(t => t.HasCheckConstraint("chk_contact", "email IS NOT NULL OR whatsapp IS NOT NULL"));

        builder.HasOne(e => e.Admin)
            .WithMany(a => a.Employees)
            .HasForeignKey(e => e.AdminId);
    }
}
