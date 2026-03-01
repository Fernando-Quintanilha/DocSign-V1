using FluentAssertions;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using HoleriteSign.Infrastructure.Data;

namespace HoleriteSign.Tests.Services;

public class EmployeeServiceTests
{
    private EmployeeService CreateService(out AppDbContext db)
    {
        db = TestDbHelper.CreateInMemoryDb();
        TestDbHelper.SeedAsync(db).GetAwaiter().GetResult();
        var config = TestDbHelper.CreateTestConfig();
        var encryption = new EncryptionService(config);
        return new EmployeeService(db, encryption);
    }

    // ── List ──────────────────────────────────────────────

    [Fact]
    public async Task List_ReturnsAllActiveEmployees()
    {
        var svc = CreateService(out _);
        var list = await svc.ListAsync(TestDbHelper.AdminId);

        list.Should().HaveCount(1);
        list[0].Name.Should().Be("Maria Silva");
    }

    [Fact]
    public async Task List_ExcludeSoftDeleted()
    {
        var svc = CreateService(out var db);

        // Soft-delete the employee
        var emp = await db.Employees.FindAsync(TestDbHelper.EmployeeId);
        emp!.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var list = await svc.ListAsync(TestDbHelper.AdminId);
        list.Should().BeEmpty();
    }

    // ── GetById ───────────────────────────────────────────

    [Fact]
    public async Task GetById_Existing_ReturnsEmployee()
    {
        var svc = CreateService(out _);
        var result = await svc.GetByIdAsync(TestDbHelper.EmployeeId, TestDbHelper.AdminId);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Maria Silva");
        result.CpfLast4.Should().Be("1234");
        result.HasBirthDate.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.GetByIdAsync(Guid.NewGuid(), TestDbHelper.AdminId);
        result.Should().BeNull();
    }

    // ── Create ────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidRequest_CreatesEmployee()
    {
        var svc = CreateService(out _);
        var req = new CreateEmployeeRequest("Pedro Santos", "pedro@emp.com", null, "12345678901", "1985-03-20");

        var result = await svc.CreateAsync(req, TestDbHelper.AdminId);

        result.Should().NotBeNull();
        result.Name.Should().Be("Pedro Santos");
        result.Email.Should().Be("pedro@emp.com");
        result.CpfLast4.Should().Be("8901");
        result.HasBirthDate.Should().BeTrue();
    }

    [Fact]
    public async Task Create_NoContactMethod_Throws()
    {
        var svc = CreateService(out _);
        var req = new CreateEmployeeRequest("Sem Contato", null, null, null, null);

        await svc.Invoking(s => s.CreateAsync(req, TestDbHelper.AdminId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*e-mail ou WhatsApp*");
    }

    [Fact]
    public async Task Create_InvalidCpf_Throws()
    {
        var svc = CreateService(out _);
        var req = new CreateEmployeeRequest("Bad CPF", "a@b.com", null, "123", null);

        await svc.Invoking(s => s.CreateAsync(req, TestDbHelper.AdminId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*11 dígitos*");
    }

    [Fact]
    public async Task Create_InvalidBirthDate_Throws()
    {
        var svc = CreateService(out _);
        var req = new CreateEmployeeRequest("Bad Date", "c@d.com", null, null, "not-a-date");

        await svc.Invoking(s => s.CreateAsync(req, TestDbHelper.AdminId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*nascimento*");
    }

    [Fact]
    public async Task Create_EmailTrimmedAndLowered()
    {
        var svc = CreateService(out _);
        var req = new CreateEmployeeRequest("Test", " FOO@BAR.COM ", null, null, null);

        var result = await svc.CreateAsync(req, TestDbHelper.AdminId);
        result.Email.Should().Be("foo@bar.com");
    }

    // ── Update ────────────────────────────────────────────

    [Fact]
    public async Task Update_ValidRequest_UpdatesEmployee()
    {
        var svc = CreateService(out _);
        var req = new UpdateEmployeeRequest("Maria Souza", "maria.new@emp.com", "+5511888888888", null, null);

        var result = await svc.UpdateAsync(TestDbHelper.EmployeeId, req, TestDbHelper.AdminId);

        result.Name.Should().Be("Maria Souza");
        result.Email.Should().Be("maria.new@emp.com");
    }

    [Fact]
    public async Task Update_NonExistent_Throws()
    {
        var svc = CreateService(out _);
        var req = new UpdateEmployeeRequest("X", "x@x.com", null, null, null);

        await svc.Invoking(s => s.UpdateAsync(Guid.NewGuid(), req, TestDbHelper.AdminId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não encontrado*");
    }

    // ── Delete ────────────────────────────────────────────

    [Fact]
    public async Task Delete_SoftDeletesEmployee()
    {
        var svc = CreateService(out var db);

        await svc.DeleteAsync(TestDbHelper.EmployeeId, TestDbHelper.AdminId);

        var emp = await db.Employees.FindAsync(TestDbHelper.EmployeeId);
        emp!.IsActive.Should().BeFalse();
        emp.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_NonExistent_Throws()
    {
        var svc = CreateService(out _);

        await svc.Invoking(s => s.DeleteAsync(Guid.NewGuid(), TestDbHelper.AdminId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não encontrado*");
    }
}
