using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HoleriteSign.Tests;

/// <summary>
/// Helper to create an in-memory AppDbContext seeded with base data.
/// </summary>
public static class TestDbHelper
{
    public static readonly Guid FreePlanId = Guid.Parse("aaaa0000-0000-0000-0000-000000000001");
    public static readonly Guid AdminId = Guid.Parse("bbbb0000-0000-0000-0000-000000000001");
    public static readonly Guid EmployeeId = Guid.Parse("cccc0000-0000-0000-0000-000000000001");
    public static readonly Guid PayPeriodId = Guid.Parse("dddd0000-0000-0000-0000-000000000001");
    public static readonly Guid DocumentId = Guid.Parse("eeee0000-0000-0000-0000-000000000001");

    /// <summary>
    /// Creates a fresh in-memory DB with no tenant filter (suitable for unit tests).
    /// </summary>
    public static AppDbContext CreateInMemoryDb(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>
    /// Seeds the DB with a Plan, Admin, Employee, PayPeriod, and Document.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db)
    {
        var plan = new Plan
        {
            Id = FreePlanId,
            Name = "free",
            DisplayName = "Plano Gratuito",
            MaxDocuments = 10,
            MaxEmployees = 5,
            PriceMonthly = 0,
            IsActive = true,
        };

        var admin = new Admin
        {
            Id = AdminId,
            Name = "João Teste",
            Email = "joao@teste.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Senha@123"),
            CompanyName = "Empresa Teste LTDA",
            PlanId = FreePlanId,
            Role = AdminRole.Admin,
            EmailVerified = true,
            IsActive = true,
        };

        var employee = new Employee
        {
            Id = EmployeeId,
            AdminId = AdminId,
            Name = "Maria Silva",
            Email = "maria@empresa.com",
            WhatsApp = "+5511999999999",
            CpfLast4 = "1234",
            CpfEncrypted = System.Text.Encoding.UTF8.GetBytes("12345671234"),
            BirthDateEncrypted = System.Text.Encoding.UTF8.GetBytes("1990-05-15"),
            IsActive = true,
        };

        var payPeriod = new PayPeriod
        {
            Id = PayPeriodId,
            AdminId = AdminId,
            Year = 2026,
            Month = 2,
            Label = "Fevereiro 2026",
        };

        var document = new Document
        {
            Id = DocumentId,
            EmployeeId = EmployeeId,
            PayPeriodId = PayPeriodId,
            AdminId = AdminId,
            OriginalFilename = "holerite_maria_fev2026.pdf",
            OriginalFileKey = $"{AdminId}/documents/{DocumentId}/holerite.pdf",
            OriginalFileHash = "abc123hash",
            FileSizeBytes = 50_000,
            Status = DocumentStatus.Uploaded,
        };

        db.Plans.Add(plan);
        db.Admins.Add(admin);
        db.Employees.Add(employee);
        db.PayPeriods.Add(payPeriod);
        db.Documents.Add(document);

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a minimal IConfiguration for test services.
    /// </summary>
    public static IConfiguration CreateTestConfig()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "TestSecretKeyThatIsAtLeast32CharsLong!!!",
            ["Jwt:Issuer"] = "HoleriteSign",
            ["Jwt:Audience"] = "HoleriteSign",
            ["Jwt:ExpirationMinutes"] = "60",
            ["App:FrontendUrl"] = "http://localhost:5173",
            ["Encryption:Key"] = "TestEncryptionKeyForUnitTests32Chars!!",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
