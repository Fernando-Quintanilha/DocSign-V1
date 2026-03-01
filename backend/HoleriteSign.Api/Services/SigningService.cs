using System.Security.Cryptography;
using System.Text;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Core.Interfaces;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

public class SigningService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IConfiguration _config;
    private readonly IStorageService _storage;
    private readonly SignedPdfService _pdfService;
    private readonly EncryptionService _encryption;

    public SigningService(AppDbContext db, IAuditService audit, IConfiguration config, IStorageService storage, EncryptionService encryption, SignedPdfService? pdfService = null)
    {
        _db = db;
        _audit = audit;
        _config = config;
        _storage = storage;
        _encryption = encryption;
        _pdfService = pdfService!;
    }

    /// <summary>
    /// Admin generates a signing token for a document.
    /// Returns the signing URL and expiration info.
    /// </summary>
    public async Task<GenerateTokenResponse> GenerateTokenAsync(Guid documentId, Guid adminId)
    {
        var document = await _db.Documents
            .IgnoreQueryFilters()
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.AdminId == adminId)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        if (document.Status == DocumentStatus.Signed)
            throw new InvalidOperationException("Documento já foi assinado.");

        // Generate secure token
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        // Store hash only (SEC-15)
        var tokenHash = ComputeHash(rawToken);
        var expiresAt = DateTime.UtcNow.AddHours(72); // 3 days

        document.SigningTokenHash = tokenHash;
        document.TokenExpiresAt = expiresAt;
        document.TokenUsedAt = null;
        document.Status = DocumentStatus.Sent;
        document.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Audit
        await _audit.LogAsync(
            "token_generated",
            ActorType.Admin,
            adminId: adminId,
            documentId: documentId,
            employeeId: document.EmployeeId);

        var frontendUrl = _config["App:FrontendUrl"] ?? "http://localhost:5173";
        var signingUrl = $"{frontendUrl}/sign/{rawToken}";

        return new GenerateTokenResponse(signingUrl, expiresAt);
    }

    /// <summary>
    /// Public: validate a signing token without consuming it.
    /// </summary>
    public async Task<ValidateTokenResponse?> ValidateTokenAsync(string token)
    {
        var tokenHash = ComputeHash(token);
        var document = await _db.Documents
            .IgnoreQueryFilters()
            .Include(d => d.Employee)
            .Include(d => d.Admin)
            .Include(d => d.PayPeriod)
            .FirstOrDefaultAsync(d => d.SigningTokenHash == tokenHash);

        if (document is null)
            return null;

        if (document.TokenExpiresAt < DateTime.UtcNow)
            return null;

        if (document.Status == DocumentStatus.Signed)
            return null;

        var hasCpf = document.Employee.CpfEncrypted != null && document.Employee.CpfEncrypted.Length > 0;
        var hasDob = document.Employee.BirthDateEncrypted != null && document.Employee.BirthDateEncrypted.Length > 0;

        return new ValidateTokenResponse(
            Valid: true,
            EmployeeName: document.Employee.Name,
            CompanyName: document.Admin.CompanyName,
            PayPeriodLabel: document.PayPeriod.Label ?? $"{document.PayPeriod.Month:D2}/{document.PayPeriod.Year}",
            RequiresCpf: hasCpf,
            RequiresBirthDate: hasDob
        );
    }

    /// <summary>
    /// Public: verify employee identity (CPF + Birth Date).
    /// For MVP: if no CPF/DOB is stored, auto-verify.
    /// </summary>
    public async Task<VerifyIdentityResponse> VerifyIdentityAsync(
        string token, VerifyIdentityRequest request, string ip, string userAgent)
    {
        var tokenHash = ComputeHash(token);
        var document = await _db.Documents
            .IgnoreQueryFilters()
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.SigningTokenHash == tokenHash);

        if (document is null || document.TokenExpiresAt < DateTime.UtcNow || document.Status == DocumentStatus.Signed)
            return new VerifyIdentityResponse(false, "Token inválido ou expirado.");

        var employee = document.Employee;
        var verified = true;

        // Verify CPF if stored
        if (employee.CpfEncrypted != null && employee.CpfEncrypted.Length > 0)
        {
            if (string.IsNullOrEmpty(request.Cpf))
                return new VerifyIdentityResponse(false, "CPF é obrigatório.");

            var cpfDigits = new string(request.Cpf.Where(char.IsDigit).ToArray());
            if (cpfDigits.Length != 11)
                return new VerifyIdentityResponse(false, "CPF inválido.");

            // Decrypt stored CPF and compare full value
            var storedCpf = _encryption.Decrypt(employee.CpfEncrypted);
            if (storedCpf != cpfDigits)
            {
                await _audit.LogAsync("identity_verification_failed", ActorType.Employee,
                    adminId: document.AdminId,
                    employeeId: employee.Id, documentId: document.Id,
                    eventData: "{\"reason\":\"cpf_mismatch\"}", actorIp: ip, actorUserAgent: userAgent);
                verified = false;
            }
        }

        // Verify Birth Date if stored
        if (verified && employee.BirthDateEncrypted != null && employee.BirthDateEncrypted.Length > 0)
        {
            if (string.IsNullOrEmpty(request.BirthDate))
                return new VerifyIdentityResponse(false, "Data de nascimento é obrigatória.");

            var storedDob = _encryption.Decrypt(employee.BirthDateEncrypted);
            // Normalize both to yyyy-MM-dd for comparison
            if (!DateOnly.TryParse(request.BirthDate, out var requestDob) ||
                !DateOnly.TryParse(storedDob, out var savedDob) ||
                requestDob != savedDob)
            {
                await _audit.LogAsync("identity_verification_failed", ActorType.Employee,
                    adminId: document.AdminId,
                    employeeId: employee.Id, documentId: document.Id,
                    eventData: "{\"reason\":\"dob_mismatch\"}", actorIp: ip, actorUserAgent: userAgent);
                verified = false;
            }
        }

        if (!verified)
            return new VerifyIdentityResponse(false, "Dados não conferem. Verifique e tente novamente.");

        // Mark as viewed
        if (document.ViewedAt == null)
        {
            document.ViewedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // Create or update verification record
        var verification = await _db.SigningVerifications
            .FirstOrDefaultAsync(v => v.DocumentId == document.Id);

        if (verification == null)
        {
            verification = new SigningVerification
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                EmployeeId = employee.Id,
                Method = VerificationMethod.Cpf,
                Verified = true,
                VerifiedAt = DateTime.UtcNow,
                ExpiresAt = document.TokenExpiresAt!.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            _db.SigningVerifications.Add(verification);
        }
        else
        {
            verification.Verified = true;
            verification.VerifiedAt = DateTime.UtcNow;
            verification.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("identity_verified", ActorType.Employee,
            adminId: document.AdminId,
            employeeId: employee.Id, documentId: document.Id,
            actorIp: ip, actorUserAgent: userAgent);

        return new VerifyIdentityResponse(true, "Identidade verificada com sucesso.");
    }

    /// <summary>
    /// Public: get document info for viewing after identity verification.
    /// </summary>
    public async Task<SigningDocumentDto?> GetDocumentForSigningAsync(string token)
    {
        var tokenHash = ComputeHash(token);
        var document = await _db.Documents
            .IgnoreQueryFilters()
            .Include(d => d.Employee)
            .Include(d => d.Admin)
            .Include(d => d.PayPeriod)
            .FirstOrDefaultAsync(d => d.SigningTokenHash == tokenHash);

        if (document is null || document.TokenExpiresAt < DateTime.UtcNow || document.Status == DocumentStatus.Signed)
            return null;

        // Check if identity was verified
        var verification = await _db.SigningVerifications
            .FirstOrDefaultAsync(v => v.DocumentId == document.Id && v.Verified);

        // MVP: allow access even without verification if employee has no CPF stored
        var hasCpf = document.Employee.CpfEncrypted != null && document.Employee.CpfEncrypted.Length > 0;
        if (hasCpf && verification == null)
            return null;

        // Generate download URL (MVP: direct file serving)
        // Frontend prepends /api via proxy — return only the route
        var downloadUrl = $"/signing/download/{token}";

        return new SigningDocumentDto(
            document.Id,
            document.Employee.Name,
            document.Admin.CompanyName,
            document.PayPeriod.Label ?? $"{document.PayPeriod.Month:D2}/{document.PayPeriod.Year}",
            document.OriginalFilename,
            document.FileSizeBytes,
            downloadUrl
        );
    }

    /// <summary>
    /// Public: get the actual file bytes for download/viewing.
    /// </summary>
    public async Task<(byte[] bytes, string filename)?> GetFileForDownloadAsync(string token)
    {
        var tokenHash = ComputeHash(token);
        var document = await _db.Documents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.SigningTokenHash == tokenHash);

        if (document is null || document.TokenExpiresAt < DateTime.UtcNow)
            return null;

        try
        {
            var bytes = await _storage.DownloadAsync(document.OriginalFileKey);
            return (bytes, document.OriginalFilename);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Public: complete the signature with selfie photo.
    /// </summary>
    public async Task<SignDocumentResponse> SignAsync(
        string token, SignDocumentRequest request, string ip, string userAgent)
    {
        if (!request.ConsentGiven)
            return new SignDocumentResponse(false, "É necessário aceitar os termos para assinar.", DateTime.MinValue);

        var tokenHash = ComputeHash(token);
        var document = await _db.Documents
            .IgnoreQueryFilters()
            .Include(d => d.Employee)
            .Include(d => d.Signature)
            .FirstOrDefaultAsync(d => d.SigningTokenHash == tokenHash);

        if (document is null || document.TokenExpiresAt < DateTime.UtcNow)
            return new SignDocumentResponse(false, "Token inválido ou expirado.", DateTime.MinValue);

        if (document.Status == DocumentStatus.Signed || document.Signature != null)
            return new SignDocumentResponse(false, "Documento já foi assinado.", DateTime.MinValue);

        // Decode and save selfie photo
        byte[] photoBytes;
        try
        {
            photoBytes = Convert.FromBase64String(request.PhotoBase64);
        }
        catch
        {
            return new SignDocumentResponse(false, "Foto inválida.", DateTime.MinValue);
        }

        if (photoBytes.Length < 1024) // At least 1KB
            return new SignDocumentResponse(false, "Foto muito pequena. Tire uma selfie válida.", DateTime.MinValue);

        if (photoBytes.Length > 5 * 1024 * 1024) // Max 5MB
            return new SignDocumentResponse(false, "Foto excede 5 MB.", DateTime.MinValue);

        // Save photo file
        var photoHash = Convert.ToHexString(SHA256.HashData(photoBytes)).ToLowerInvariant();
        var ext = request.PhotoMimeType.Contains("png") ? "png" : "jpg";
        var photoKey = $"{document.AdminId}/signatures/{document.Id}/{Guid.NewGuid()}.{ext}";

        await _storage.UploadBytesAsync(photoKey, photoBytes, request.PhotoMimeType);

        var signedAt = DateTime.UtcNow;

        // Create signature record
        var signature = new Signature
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            EmployeeId = document.EmployeeId,
            PhotoFileKey = photoKey,
            PhotoHash = photoHash,
            PhotoMimeType = request.PhotoMimeType,
            SignerIp = ip,
            SignerUserAgent = userAgent,
            SignerGeolocation = request.Geolocation,
            SignedAt = signedAt,
            ConsentGiven = true,
            ConsentText = "Declaro que li e concordo com o conteúdo do holerite apresentado. " +
                          "Confirmo minha identidade por meio da selfie capturada neste momento.",
            CreatedAt = signedAt,
        };

        _db.Signatures.Add(signature);

        // Update document status
        document.Status = DocumentStatus.Signed;
        document.TokenUsedAt = signedAt;
        document.UpdatedAt = signedAt;

        await _db.SaveChangesAsync();

        // Generate signed PDF with embedded photo and audit trail
        try
        {
            var signedFileKey = $"{document.AdminId}/signed/{document.Id}/{Guid.NewGuid()}.pdf";
            var periodLabel = await _db.PayPeriods
                .Where(p => p.Id == document.PayPeriodId)
                .Select(p => p.Label ?? $"{p.Month:D2}/{p.Year}")
                .FirstOrDefaultAsync() ?? "";
            var companyName = await _db.Admins
                .IgnoreQueryFilters()
                .Where(a => a.Id == document.AdminId)
                .Select(a => a.CompanyName)
                .FirstOrDefaultAsync() ?? "";

            var (pdfBytes, key) = await _pdfService.GenerateSignedPdfAsync(
                document.OriginalFileKey,
                signedFileKey,
                document.Employee.Name,
                companyName,
                periodLabel,
                document.OriginalFilename,
                photoBytes,
                request.PhotoMimeType,
                photoHash,
                ip,
                userAgent,
                signedAt,
                signature.ConsentText);

            document.SignedFileKey = key;
            document.SignedFileHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(pdfBytes)).ToLowerInvariant();
            await _db.SaveChangesAsync();
        }
        catch
        {
            // PDF generation failure should not block signature
        }

        // Audit
        await _audit.LogAsync("document_signed", ActorType.Employee,
            adminId: document.AdminId,
            employeeId: document.EmployeeId,
            documentId: document.Id,
            eventData: $"{{\"ip\":\"{ip}\",\"photoHash\":\"{photoHash}\"}}",
            actorIp: ip,
            actorUserAgent: userAgent);

        return new SignDocumentResponse(true, "Holerite assinado com sucesso!", signedAt);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
