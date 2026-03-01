using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentTenantService? _tenantService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService? tenantService = null)
        : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PayPeriod> PayPeriods => Set<PayPeriod>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Signature> Signatures => Set<Signature>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SigningVerification> SigningVerifications => Set<SigningVerification>();

    // Property used by query filters — evaluated per-query, not once
    public Guid CurrentAdminId => _tenantService?.AdminId ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // =========================================================
        // SEC-11: Global Query Filters for tenant isolation
        // The filters reference 'this.CurrentAdminId' so EF Core
        // re-evaluates per DbContext instance (per request).
        // =========================================================
        modelBuilder.Entity<Employee>().HasQueryFilter(
            e => CurrentAdminId == Guid.Empty || e.AdminId == CurrentAdminId);
        modelBuilder.Entity<PayPeriod>().HasQueryFilter(
            p => CurrentAdminId == Guid.Empty || p.AdminId == CurrentAdminId);
        modelBuilder.Entity<Document>().HasQueryFilter(
            d => CurrentAdminId == Guid.Empty || d.AdminId == CurrentAdminId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(
            a => CurrentAdminId == Guid.Empty || a.AdminId == CurrentAdminId);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity is Admin admin)
                admin.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Employee employee)
                employee.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is Document document)
                document.UpdatedAt = DateTime.UtcNow;
            else if (entry.Entity is SigningVerification verification)
                verification.UpdatedAt = DateTime.UtcNow;
        }
    }
}
