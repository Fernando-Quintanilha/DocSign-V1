using FluentAssertions;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoleriteSign.Tests.Services;

public class AuthServiceTests
{
    private AuthService CreateService(out Infrastructure.Data.AppDbContext db)
    {
        db = TestDbHelper.CreateInMemoryDb();
        TestDbHelper.SeedAsync(db).GetAwaiter().GetResult();
        var config = TestDbHelper.CreateTestConfig();
        var emailService = new EmailService(config, NullLogger<EmailService>.Instance);
        return new AuthService(db, config, emailService);
    }

    // ── Register ──────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_ReturnsTokenAndAdmin()
    {
        var svc = CreateService(out var db);
        var req = new RegisterRequest("Novo Admin", "novo@teste.com", "Senha@123", "Nova Empresa");

        var result = await svc.RegisterAsync(req);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Admin.Email.Should().Be("novo@teste.com");
        result.Admin.CompanyName.Should().Be("Nova Empresa");
        result.Admin.PlanName.Should().Be("Plano Gratuito");
        result.Admin.EmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsInvalidOperation()
    {
        var svc = CreateService(out _);
        var req = new RegisterRequest("Dup", "joao@teste.com", "Senha@123", "Dup Corp");

        await svc.Invoking(s => s.RegisterAsync(req))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já cadastrado*");
    }

    [Fact]
    public async Task Register_ShortPassword_ThrowsInvalidOperation()
    {
        var svc = CreateService(out _);
        var req = new RegisterRequest("Admin", "x@x.com", "123", "Corp");

        await svc.Invoking(s => s.RegisterAsync(req))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*mínimo 8*");
    }

    [Fact]
    public async Task Register_EmailIsTrimmedAndLowered()
    {
        var svc = CreateService(out var db);
        var req = new RegisterRequest("Admin", " TEST@Email.COM ", "Senha@123", "Corp");

        var result = await svc.RegisterAsync(req);

        result.Admin.Email.Should().Be("test@email.com");
    }

    // ── Login ─────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var svc = CreateService(out _);
        var req = new LoginRequest("joao@teste.com", "Senha@123");

        var result = await svc.LoginAsync(req);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.Admin.Email.Should().Be("joao@teste.com");
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        var svc = CreateService(out _);
        var req = new LoginRequest("joao@teste.com", "SenhaErrada");

        await svc.Invoking(s => s.LoginAsync(req))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*inválidos*");
    }

    [Fact]
    public async Task Login_NonExistentEmail_ThrowsUnauthorized()
    {
        var svc = CreateService(out _);
        var req = new LoginRequest("naoexiste@teste.com", "Senha@123");

        await svc.Invoking(s => s.LoginAsync(req))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Login_InactiveAccount_ThrowsUnauthorized()
    {
        var svc = CreateService(out var db);

        // Deactivate the admin
        var admin = await db.Admins.FindAsync(TestDbHelper.AdminId);
        admin!.IsActive = false;
        await db.SaveChangesAsync();

        var req = new LoginRequest("joao@teste.com", "Senha@123");

        await svc.Invoking(s => s.LoginAsync(req))
            .Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*desativada*");
    }

    [Fact]
    public async Task Login_JwtContainsCorrectClaims()
    {
        var svc = CreateService(out _);
        var result = await svc.LoginAsync(new LoginRequest("joao@teste.com", "Senha@123"));

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(result.Token);

        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == TestDbHelper.AdminId.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "joao@teste.com");
        jwt.Claims.Should().Contain(c => c.Type == "company" && c.Value == "Empresa Teste LTDA");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Admin");
    }
}
