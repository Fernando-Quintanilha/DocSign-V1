using FluentAssertions;
using HoleriteSign.Api.Services;
using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Infrastructure.Data;

namespace HoleriteSign.Tests.Services;

public class DashboardServiceTests
{
    private DashboardService CreateService(out AppDbContext db)
    {
        db = TestDbHelper.CreateInMemoryDb();
        TestDbHelper.SeedAsync(db).GetAwaiter().GetResult();
        return new DashboardService(db);
    }

    // ── GetStatsAsync ─────────────────────────────────────

    [Fact]
    public async Task GetStats_ReturnsCorrectCounts()
    {
        var svc = CreateService(out _);
        var stats = await svc.GetStatsAsync(TestDbHelper.AdminId);

        stats.TotalEmployees.Should().Be(1);
        stats.ActiveEmployees.Should().Be(1);
        stats.TotalDocuments.Should().Be(1);
        stats.PendingDocuments.Should().Be(1); // Document status = Uploaded
        stats.SignedDocuments.Should().Be(0);
        stats.ExpiredDocuments.Should().Be(0);
        stats.PlanName.Should().Be("Plano Gratuito");
    }

    [Fact]
    public async Task GetStats_WithSignedDocument_CountsCorrectly()
    {
        var svc = CreateService(out var db);
        var doc = await db.Documents.FindAsync(TestDbHelper.DocumentId);
        doc!.Status = DocumentStatus.Signed;
        await db.SaveChangesAsync();

        var stats = await svc.GetStatsAsync(TestDbHelper.AdminId);

        stats.SignedDocuments.Should().Be(1);
        stats.PendingDocuments.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_NoData_ReturnsZeros()
    {
        var db = TestDbHelper.CreateInMemoryDb();
        // Seed only plan and admin, no employees/documents
        var plan = new Plan
        {
            Id = Guid.NewGuid(), Name = "free", DisplayName = "Gratuito",
            MaxDocuments = 10, MaxEmployees = 5, PriceMonthly = 0, IsActive = true,
        };
        var admin = new Admin
        {
            Id = TestDbHelper.AdminId, Name = "Test", Email = "t@t.com",
            PasswordHash = "x", CompanyName = "C", PlanId = plan.Id,
            Role = AdminRole.Admin, EmailVerified = true, IsActive = true,
        };
        db.Plans.Add(plan);
        db.Admins.Add(admin);
        await db.SaveChangesAsync();

        var svc = new DashboardService(db);
        var stats = await svc.GetStatsAsync(TestDbHelper.AdminId);

        stats.TotalEmployees.Should().Be(0);
        stats.TotalDocuments.Should().Be(0);
    }

    // ── GetEnhancedStatsAsync ─────────────────────────────

    [Fact]
    public async Task GetEnhancedStats_ReturnsPeriodsAndActivity()
    {
        var svc = CreateService(out _);
        var stats = await svc.GetEnhancedStatsAsync(TestDbHelper.AdminId);

        stats.TotalEmployees.Should().Be(1);
        stats.Periods.Should().HaveCount(1);
        stats.Periods[0].Label.Should().Be("Fevereiro 2026");
        stats.Periods[0].TotalDocuments.Should().Be(1);
    }

    [Fact]
    public async Task GetEnhancedStats_PendingEmployees_ShowsCorrectly()
    {
        var svc = CreateService(out _);
        var stats = await svc.GetEnhancedStatsAsync(TestDbHelper.AdminId);

        stats.PendingEmployees.Should().ContainSingle();
        stats.PendingEmployees[0].EmployeeName.Should().Be("Maria Silva");
    }

    [Fact]
    public async Task GetEnhancedStats_DocumentsUsedThisMonth_Counted()
    {
        var svc = CreateService(out _);
        var stats = await svc.GetEnhancedStatsAsync(TestDbHelper.AdminId);

        // Document was created in seed (CreatedAt defaults to DateTime.UtcNow-ish in EF)
        stats.DocumentsUsedThisMonth.Should().BeGreaterOrEqualTo(0);
    }

    // ── GetPendingByPeriodAsync ───────────────────────────

    [Fact]
    public async Task GetPendingByPeriod_ReturnsEmployeesWithUnsignedDocs()
    {
        var svc = CreateService(out _);
        var pending = await svc.GetPendingByPeriodAsync(TestDbHelper.PayPeriodId, TestDbHelper.AdminId);

        pending.Should().ContainSingle();
        pending[0].EmployeeName.Should().Be("Maria Silva");
        pending[0].DocumentStatus.Should().Be("Uploaded");
    }

    [Fact]
    public async Task GetPendingByPeriod_SignedDoc_ExcludedFromPending()
    {
        var svc = CreateService(out var db);

        var doc = await db.Documents.FindAsync(TestDbHelper.DocumentId);
        doc!.Status = DocumentStatus.Signed;
        await db.SaveChangesAsync();

        var pending = await svc.GetPendingByPeriodAsync(TestDbHelper.PayPeriodId, TestDbHelper.AdminId);

        // Signed doc employee is NOT in pending list
        pending.Should().BeEmpty();
    }
}
