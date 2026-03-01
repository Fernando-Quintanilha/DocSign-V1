using System.Text;
using FluentAssertions;
using HoleriteSign.Api.Services;
using HoleriteSign.Infrastructure.Data;

namespace HoleriteSign.Tests.Services;

public class CsvImportTests
{
    private EmployeeService CreateService(out AppDbContext db)
    {
        db = TestDbHelper.CreateInMemoryDb();
        TestDbHelper.SeedAsync(db).GetAwaiter().GetResult();
        var config = TestDbHelper.CreateTestConfig();
        var encryption = new EncryptionService(config);
        return new EmployeeService(db, encryption);
    }

    private Stream MakeCsv(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    // ── ImportCsvAsync ────────────────────────────────────

    [Fact]
    public async Task Import_ValidCsv_CreatesEmployees()
    {
        var svc = CreateService(out var db);
        var csv = "Nome,Email,WhatsApp,CPF,DataNascimento\nCarlos Souza,carlos@test.com,,,\nAna Lima,ana@test.com,+5511888888888,,";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(2);
        result.Skipped.Should().Be(0);
        result.Errors.Should().BeEmpty();

        var employees = await svc.ListAsync(TestDbHelper.AdminId);
        employees.Should().HaveCount(3); // 1 seeded + 2 imported
    }

    [Fact]
    public async Task Import_SemicolonDelimiter_Works()
    {
        var svc = CreateService(out _);
        var csv = "Nome;Email;WhatsApp\nPaulo Reis;paulo@test.com;";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(1);
    }

    [Fact]
    public async Task Import_EmptyFile_Throws()
    {
        var svc = CreateService(out _);

        await svc.Invoking(s => s.ImportCsvAsync(MakeCsv(""), TestDbHelper.AdminId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CSV vazio*");
    }

    [Fact]
    public async Task Import_DuplicateEmail_Skips()
    {
        var svc = CreateService(out _);
        // maria@empresa.com already exists (seeded)
        var csv = "Nome,Email\nMaria Dup,maria@empresa.com";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(0);
        result.Skipped.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("já cadastrado"));
    }

    [Fact]
    public async Task Import_MissingName_Skips()
    {
        var svc = CreateService(out _);
        var csv = "Nome,Email\n,missing_name@test.com";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(0);
        result.Skipped.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("nome vazio"));
    }

    [Fact]
    public async Task Import_NoContact_Skips()
    {
        var svc = CreateService(out _);
        var csv = "Nome,Email,WhatsApp\nSem Contato,,";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(0);
        result.Skipped.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("email ou whatsapp"));
    }

    [Fact]
    public async Task Import_InsufficientColumns_Skips()
    {
        var svc = CreateService(out _);
        var csv = "Nome\nOnlyName";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(0);
        result.Skipped.Should().Be(1);
        result.Errors.Should().ContainSingle(e => e.Contains("colunas insuficientes"));
    }

    [Fact]
    public async Task Import_QuotedFields_ParsedCorrectly()
    {
        var svc = CreateService(out _);
        var csv = "Nome,Email\n\"Santos, João\",joao@test.com";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(1);

        var employees = await svc.ListAsync(TestDbHelper.AdminId);
        employees.Should().Contain(e => e.Name == "Santos, João");
    }

    [Fact]
    public async Task Import_WithCpfAndBirthDate_Creates()
    {
        var svc = CreateService(out _);
        var csv = "Nome,Email,WhatsApp,CPF,DataNascimento\nTest Full,full@test.com,,98765432100,1995-07-20";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(1);

        var employees = await svc.ListAsync(TestDbHelper.AdminId);
        employees.Should().Contain(e => e.Name == "Test Full" && e.CpfLast4 == "2100");
    }

    [Fact]
    public async Task Import_MixedValidAndInvalid_CountsCorrectly()
    {
        var svc = CreateService(out _);
        var csv = "Nome,Email,WhatsApp\nValid One,valid1@test.com,\n,,\nValid Two,valid2@test.com,";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(2);
        result.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task Import_BlankLines_Ignored()
    {
        var svc = CreateService(out _);
        var csv = "Nome,Email\n\nTest Blank,blank@test.com\n\n";

        var result = await svc.ImportCsvAsync(MakeCsv(csv), TestDbHelper.AdminId);

        result.Created.Should().Be(1);
        result.Skipped.Should().Be(0);
    }
}
