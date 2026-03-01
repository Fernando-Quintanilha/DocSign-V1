using HoleriteSign.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class PayPeriodConfiguration : IEntityTypeConfiguration<PayPeriod>
{
    public void Configure(EntityTypeBuilder<PayPeriod> builder)
    {
        builder.ToTable("pay_periods");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.AdminId).HasColumnName("admin_id");
        builder.Property(p => p.Year).HasColumnName("year");
        builder.Property(p => p.Month).HasColumnName("month");
        builder.Property(p => p.Label).HasColumnName("label").HasMaxLength(50);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        builder.HasIndex(p => new { p.AdminId, p.Year, p.Month }).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint("chk_month", "month BETWEEN 1 AND 12"));

        builder.HasOne(p => p.Admin)
            .WithMany(a => a.PayPeriods)
            .HasForeignKey(p => p.AdminId);
    }
}
