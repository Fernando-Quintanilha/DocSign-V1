using HoleriteSign.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoleriteSign.Infrastructure.Data.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(p => p.MaxDocuments).HasColumnName("max_documents").HasDefaultValue(10);
        builder.Property(p => p.MaxEmployees).HasColumnName("max_employees").HasDefaultValue(5);
        builder.Property(p => p.PriceMonthly).HasColumnName("price_monthly").HasColumnType("decimal(10,2)").HasDefaultValue(0);
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");

        // Seed default plans
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Plan
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "free",
                DisplayName = "Plano Gratuito",
                MaxDocuments = 10,
                MaxEmployees = 5,
                PriceMonthly = 0,
                IsActive = true,
                CreatedAt = seedDate
            },
            new Plan
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "basic",
                DisplayName = "Plano Básico",
                MaxDocuments = 50,
                MaxEmployees = 25,
                PriceMonthly = 49.90m,
                IsActive = true,
                CreatedAt = seedDate
            },
            new Plan
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = "pro",
                DisplayName = "Plano Profissional",
                MaxDocuments = 200,
                MaxEmployees = 100,
                PriceMonthly = 99.90m,
                IsActive = true,
                CreatedAt = seedDate
            },
            new Plan
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                Name = "enterprise",
                DisplayName = "Plano Empresarial",
                MaxDocuments = -1, // unlimited
                MaxEmployees = -1,
                PriceMonthly = 199.90m,
                IsActive = true,
                CreatedAt = seedDate
            }
        );
    }
}
