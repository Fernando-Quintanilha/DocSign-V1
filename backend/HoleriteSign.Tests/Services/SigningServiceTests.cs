using FluentAssertions;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using HoleriteSign.Core.Enums;
using HoleriteSign.Core.Interfaces;
using HoleriteSign.Infrastructure.Data;
using Moq;

namespace HoleriteSign.Tests.Services;

public class SigningServiceTests
{
    private SigningService CreateService(out AppDbContext db)
    {
        db = TestDbHelper.CreateInMemoryDb();
        TestDbHelper.SeedAsync(db).GetAwaiter().GetResult();
        var config = TestDbHelper.CreateTestConfig();
        var audit = new Mock<IAuditService>();
        var storage = new Mock<IStorageService>();
        storage.Setup(s => s.DownloadAsync(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
        storage.Setup(s => s.UploadBytesAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var encryption = new EncryptionService(config);
        return new SigningService(db, audit.Object, config, storage.Object, encryption);
    }

    // ── GenerateToken ─────────────────────────────────────

    [Fact]
    public async Task GenerateToken_ValidDocument_ReturnsUrl()
    {
        var svc = CreateService(out _);

        var result = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);

        result.Should().NotBeNull();
        result.SigningUrl.Should().StartWith("http://localhost:5173/sign/");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateToken_NonExistentDocument_Throws()
    {
        var svc = CreateService(out _);

        await svc.Invoking(s => s.GenerateTokenAsync(Guid.NewGuid(), TestDbHelper.AdminId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não encontrado*");
    }

    [Fact]
    public async Task GenerateToken_AlreadySigned_Throws()
    {
        var svc = CreateService(out var db);

        // Mark document as signed
        var doc = await db.Documents.FindAsync(TestDbHelper.DocumentId);
        doc!.Status = DocumentStatus.Signed;
        await db.SaveChangesAsync();

        await svc.Invoking(s => s.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já foi assinado*");
    }

    [Fact]
    public async Task GenerateToken_SetsStatusToSent()
    {
        var svc = CreateService(out var db);

        await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);

        var doc = await db.Documents.FindAsync(TestDbHelper.DocumentId);
        doc!.Status.Should().Be(DocumentStatus.Sent);
        doc.SigningTokenHash.Should().NotBeNullOrEmpty();
        doc.TokenExpiresAt.Should().NotBeNull();
    }

    // ── ValidateToken ─────────────────────────────────────

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsInfo()
    {
        var svc = CreateService(out _);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        var result = await svc.ValidateTokenAsync(rawToken);

        result.Should().NotBeNull();
        result!.Valid.Should().BeTrue();
        result.EmployeeName.Should().Be("Maria Silva");
        result.CompanyName.Should().Be("Empresa Teste LTDA");
        result.RequiresCpf.Should().BeTrue();
        result.RequiresBirthDate.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateToken_InvalidToken_ReturnsNull()
    {
        var svc = CreateService(out _);
        var result = await svc.ValidateTokenAsync("invalid-random-token");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_ExpiredToken_ReturnsNull()
    {
        var svc = CreateService(out var db);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        // Force expire
        var doc = await db.Documents.FindAsync(TestDbHelper.DocumentId);
        doc!.TokenExpiresAt = DateTime.UtcNow.AddHours(-1);
        await db.SaveChangesAsync();

        var result = await svc.ValidateTokenAsync(rawToken);
        result.Should().BeNull();
    }

    // ── VerifyIdentity ────────────────────────────────────

    [Fact]
    public async Task VerifyIdentity_CorrectCpf_Verifies()
    {
        var svc = CreateService(out _);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        var req = new VerifyIdentityRequest("12345671234", "1990-05-15");
        var result = await svc.VerifyIdentityAsync(rawToken, req, "127.0.0.1", "Test");

        result.Verified.Should().BeTrue();
        result.Message.Should().Contain("sucesso");
    }

    [Fact]
    public async Task VerifyIdentity_WrongCpf_Fails()
    {
        var svc = CreateService(out _);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        var req = new VerifyIdentityRequest("99999999999", null);
        var result = await svc.VerifyIdentityAsync(rawToken, req, "127.0.0.1", "Test");

        result.Verified.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyIdentity_InvalidToken_Fails()
    {
        var svc = CreateService(out _);

        var req = new VerifyIdentityRequest("12345671234", null);
        var result = await svc.VerifyIdentityAsync("bad-token", req, "127.0.0.1", "Test");

        result.Verified.Should().BeFalse();
        result.Message.Should().Contain("inválido");
    }

    // ── GetDocumentForSigning ─────────────────────────────

    [Fact]
    public async Task GetDocument_AfterVerification_ReturnsDocument()
    {
        var svc = CreateService(out _);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        // Verify identity first
        await svc.VerifyIdentityAsync(rawToken, new VerifyIdentityRequest("12345671234", "1990-05-15"), "127.0.0.1", "Test");

        var result = await svc.GetDocumentForSigningAsync(rawToken);

        result.Should().NotBeNull();
        result!.EmployeeName.Should().Be("Maria Silva");
        result.OriginalFilename.Should().Contain("holerite");
    }

    [Fact]
    public async Task GetDocument_WithoutVerification_ReturnsNull()
    {
        var svc = CreateService(out _);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        // Don't verify — should be blocked since employee has CPF
        var result = await svc.GetDocumentForSigningAsync(rawToken);
        result.Should().BeNull();
    }

    // ── Sign ──────────────────────────────────────────────

    [Fact]
    public async Task Sign_WithoutConsent_Fails()
    {
        var svc = CreateService(out _);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        var fakePhoto = Convert.ToBase64String(new byte[2048]);
        var req = new SignDocumentRequest(fakePhoto, "image/jpeg", false, null);

        var result = await svc.SignAsync(rawToken, req, "127.0.0.1", "Test");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("termos");
    }

    [Fact]
    public async Task Sign_InvalidToken_Fails()
    {
        var svc = CreateService(out _);

        var fakePhoto = Convert.ToBase64String(new byte[2048]);
        var req = new SignDocumentRequest(fakePhoto, "image/jpeg", true, null);

        var result = await svc.SignAsync("invalid-token", req, "127.0.0.1", "Test");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("inválido");
    }

    [Fact]
    public async Task Sign_TooSmallPhoto_Fails()
    {
        var svc = CreateService(out _);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        var tinyPhoto = Convert.ToBase64String(new byte[100]);
        var req = new SignDocumentRequest(tinyPhoto, "image/jpeg", true, null);

        var result = await svc.SignAsync(rawToken, req, "127.0.0.1", "Test");

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("pequena");
    }

    [Fact]
    public async Task Sign_ValidRequest_CompletesSignature()
    {
        var svc = CreateService(out var db);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        var photoBytes = new byte[2048];
        new Random(42).NextBytes(photoBytes);
        var fakePhoto = Convert.ToBase64String(photoBytes);
        var req = new SignDocumentRequest(fakePhoto, "image/jpeg", true, null);

        var result = await svc.SignAsync(rawToken, req, "192.168.1.1", "Mozilla/5.0 Test");

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("sucesso");
        result.SignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify document status changed
        var doc = await db.Documents.FindAsync(TestDbHelper.DocumentId);
        doc!.Status.Should().Be(DocumentStatus.Signed);
    }

    [Fact]
    public async Task Sign_AlreadySigned_Fails()
    {
        var svc = CreateService(out var db);
        var gen = await svc.GenerateTokenAsync(TestDbHelper.DocumentId, TestDbHelper.AdminId);
        var rawToken = gen.SigningUrl.Split("/sign/")[1];

        // Sign once
        var photoBytes = new byte[2048];
        new Random(42).NextBytes(photoBytes);
        var req = new SignDocumentRequest(Convert.ToBase64String(photoBytes), "image/jpeg", true, null);
        await svc.SignAsync(rawToken, req, "127.0.0.1", "Test");

        // Try to sign again
        var result = await svc.SignAsync(rawToken, req, "127.0.0.1", "Test");
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("já foi assinado");
    }
}
