using System.IO.Compression;
using System.Text;
using FluentAssertions;
using HoleriteSign.Api.Services;
using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Core.Interfaces;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HoleriteSign.Tests.Services;

public class ExportServiceTests
{
    private static readonly byte[] FakePdfBytes = Encoding.UTF8.GetBytes("%PDF-fake-content");

    private ExportService CreateService(out AppDbContext db, Mock<IStorageService>? storageMock = null)
    {
        db = TestDbHelper.CreateInMemoryDb();
        TestDbHelper.SeedAsync(db).GetAwaiter().GetResult();
        var storage = storageMock ?? new Mock<IStorageService>();
        return new ExportService(db, storage.Object);
    }

    // ── ExportEmployeesAsCsvAsync ─────────────────────────

    [Fact]
    public async Task ExportEmployeesCsv_ContainsExpectedHeaders()
    {
        var svc = CreateService(out _);

        var result = await svc.ExportEmployeesAsCsvAsync(TestDbHelper.AdminId);

        result.Should().NotBeNull();
        var content = Encoding.UTF8.GetString(result!);
        content.Should().Contain("Nome");
        content.Should().Contain("Email");
        content.Should().Contain("WhatsApp");
    }

    [Fact]
    public async Task ExportEmployeesCsv_ContainsEmployeeData()
    {
        var svc = CreateService(out _);

        var result = await svc.ExportEmployeesAsCsvAsync(TestDbHelper.AdminId);

        var content = Encoding.UTF8.GetString(result!);
        content.Should().Contain("Maria Silva");
        content.Should().Contain("maria@empresa.com");
    }

    [Fact]
    public async Task ExportEmployeesCsv_NoEmployees_ReturnsHeaderOnly()
    {
        var db = TestDbHelper.CreateInMemoryDb();
        var plan = new Plan
        {
            Id = Guid.NewGuid(), Name = "free", DisplayName = "Free",
            MaxDocuments = 10, MaxEmployees = 5, PriceMonthly = 0, IsActive = true,
        };
        var admin = new Admin
        {
            Id = TestDbHelper.AdminId, Name = "Test", Email = "test@test.com",
            PasswordHash = "x", CompanyName = "C", PlanId = plan.Id,
            Role = AdminRole.Admin, EmailVerified = true, IsActive = true,
        };
        db.Plans.Add(plan);
        db.Admins.Add(admin);
        await db.SaveChangesAsync();

        var storage = new Mock<IStorageService>();
        var svc = new ExportService(db, storage.Object);

        var result = await svc.ExportEmployeesAsCsvAsync(TestDbHelper.AdminId);

        result.Should().NotBeNull();
        var content = Encoding.UTF8.GetString(result!);
        content.Should().Contain("Nome"); // Header still present
        var lines = content.Trim().Split('\n');
        lines.Should().HaveCount(1); // Only header
    }

    // ── ExportAuditLogsAsCsvAsync ─────────────────────────

    [Fact]
    public async Task ExportAuditLogsCsv_ContainsHeaders()
    {
        var svc = CreateService(out var db);

        // Add an audit log entry so CSV has data
        db.AuditLogs.Add(new AuditLog
        {
            AdminId = TestDbHelper.AdminId,
            EventType = "TestEvent",
            ActorType = ActorType.Admin,
            ActorIp = "10.0.0.1",
            EntryHash = "hash1",
        });
        await db.SaveChangesAsync();

        var result = await svc.ExportAuditLogsAsCsvAsync(TestDbHelper.AdminId);

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);
        // The CSV has a UTF-8 BOM prefix, so use proper decoding
        var content = Encoding.UTF8.GetString(result);
        content.Should().Contain("Evento");
        content.Should().Contain("Tipo Ator");
    }

    [Fact]
    public async Task ExportAuditLogsCsv_WithAuditEntries_IncludesData()
    {
        var svc = CreateService(out var db);

        db.AuditLogs.Add(new AuditLog
        {
            AdminId = TestDbHelper.AdminId,
            EventType = "DocumentUploaded",
            ActorType = ActorType.Admin,
            ActorIp = "127.0.0.1",
            DocumentId = TestDbHelper.DocumentId,
            EntryHash = "test-hash",
        });
        await db.SaveChangesAsync();

        var result = await svc.ExportAuditLogsAsCsvAsync(TestDbHelper.AdminId);

        var content = Encoding.UTF8.GetString(result!);
        content.Should().Contain("DocumentUploaded");
    }

    // ── ExportSignedPdfsByPeriodAsync ──────────────────────

    [Fact]
    public async Task ExportSignedPdfs_NoPeriod_ReturnsNull()
    {
        var svc = CreateService(out _);

        var result = await svc.ExportSignedPdfsByPeriodAsync(Guid.NewGuid(), TestDbHelper.AdminId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExportSignedPdfs_NoSignedDocs_ReturnsNull()
    {
        var svc = CreateService(out _);
        // The seeded document is "Uploaded", not "Signed"

        var result = await svc.ExportSignedPdfsByPeriodAsync(TestDbHelper.PayPeriodId, TestDbHelper.AdminId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExportSignedPdfs_WithSignedDoc_ReturnsZip()
    {
        var storageMock = new Mock<IStorageService>();
        storageMock.Setup(s => s.DownloadAsync(It.IsAny<string>()))
            .ReturnsAsync(FakePdfBytes);

        var svc = CreateService(out var db, storageMock);

        // Mark document as signed
        var doc = await db.Documents.FindAsync(TestDbHelper.DocumentId);
        doc!.Status = DocumentStatus.Signed;
        doc.SignedFileKey = "signed/test.pdf";
        await db.SaveChangesAsync();

        var result = await svc.ExportSignedPdfsByPeriodAsync(TestDbHelper.PayPeriodId, TestDbHelper.AdminId);

        result.Should().NotBeNull();
        // Verify it's a valid ZIP
        using var ms = new MemoryStream(result!.Value.ZipBytes);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        zip.Entries.Should().HaveCount(1);
    }

    // ── ExportAllDataAsync ────────────────────────────────

    [Fact]
    public async Task ExportAllData_ReturnsZipWithCsvs()
    {
        var storageMock = new Mock<IStorageService>();
        storageMock.Setup(s => s.DownloadAsync(It.IsAny<string>()))
            .ReturnsAsync(FakePdfBytes);

        var svc = CreateService(out _, storageMock);

        var result = await svc.ExportAllDataAsync(TestDbHelper.AdminId);

        result.ZipBytes.Should().NotBeNull();
        using var ms = new MemoryStream(result.ZipBytes);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
        zip.Entries.Should().Contain(e => e.Name == "funcionarios.csv");
        zip.Entries.Should().Contain(e => e.Name == "auditoria.csv");
    }
}
