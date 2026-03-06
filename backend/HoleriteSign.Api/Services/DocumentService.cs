using System.Security.Cryptography;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Core.Interfaces;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

public class DocumentService
{
    private readonly AppDbContext _db;
    private readonly PayPeriodService _payPeriodService;
    private readonly IStorageService _storage;
    private readonly SignedPdfService _pdfService;

    public DocumentService(AppDbContext db, PayPeriodService payPeriodService, IStorageService storage, SignedPdfService pdfService)
    {
        _db = db;
        _payPeriodService = payPeriodService;
        _storage = storage;
        _pdfService = pdfService;
    }

    public async Task<List<DocumentDto>> ListAsync(Guid adminId, Guid? employeeId = null, Guid? payPeriodId = null)
    {
        var query = _db.Documents
            .Include(d => d.Employee)
            .Include(d => d.PayPeriod)
            .Where(d => d.AdminId == adminId);

        if (employeeId.HasValue)
            query = query.Where(d => d.EmployeeId == employeeId.Value);

        if (payPeriodId.HasValue)
            query = query.Where(d => d.PayPeriodId == payPeriodId.Value);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DocumentDto(
                d.Id,
                d.EmployeeId,
                d.Employee.Name,
                d.PayPeriodId,
                d.PayPeriod.Label ?? "",
                d.OriginalFilename,
                d.FileSizeBytes,
                d.Status.ToString(),
                d.ViewedAt,
                d.LastNotifiedAt,
                d.CreatedAt,
                d.Signature != null ? d.Signature.SignedAt : null
            ))
            .ToListAsync();
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id, Guid adminId)
    {
        return await _db.Documents
            .Include(d => d.Employee)
            .Include(d => d.PayPeriod)
            .Include(d => d.Signature)
            .Where(d => d.Id == id && d.AdminId == adminId)
            .Select(d => new DocumentDto(
                d.Id,
                d.EmployeeId,
                d.Employee.Name,
                d.PayPeriodId,
                d.PayPeriod.Label ?? "",
                d.OriginalFilename,
                d.FileSizeBytes,
                d.Status.ToString(),
                d.ViewedAt,
                d.LastNotifiedAt,
                d.CreatedAt,
                d.Signature != null ? d.Signature.SignedAt : null
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<DocumentDto> UploadAsync(IFormFile file, Guid employeeId, int year, int month, Guid adminId)
    {
        // ── Plan limit enforcement ──
        var admin = await _db.Admins.Include(a => a.Plan).FirstOrDefaultAsync(a => a.Id == adminId)
            ?? throw new KeyNotFoundException($"Admin {adminId} não encontrado.");
        if (admin.Plan.MaxDocuments > 0)
        {
            var now = DateTime.UtcNow;
            var docsThisMonth = await _db.Documents
                .Where(d => d.AdminId == adminId && d.CreatedAt.Year == now.Year && d.CreatedAt.Month == now.Month)
                .CountAsync();
            if (docsThisMonth >= admin.Plan.MaxDocuments)
                throw new InvalidOperationException(
                    $"Limite do plano atingido ({admin.Plan.MaxDocuments} documentos/mês). Faça upgrade para continuar.");
        }

        // Validate file
        if (file.Length == 0)
            throw new InvalidOperationException("Arquivo vazio.");

        if (file.Length > 10 * 1024 * 1024) // 10 MB
            throw new InvalidOperationException("Arquivo excede o limite de 10 MB.");

        var allowedTypes = new[] { "application/pdf" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            throw new InvalidOperationException("Apenas arquivos PDF são aceitos.");

        // Validate employee exists
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId && !e.DeletedAt.HasValue)
            ?? throw new InvalidOperationException("Funcionário não encontrado.");

        // Get or create pay period
        var payPeriod = await _payPeriodService.GetOrCreateAsync(year, month, adminId);

        // Check for duplicate document
        var duplicate = await _db.Documents
            .AnyAsync(d => d.EmployeeId == employeeId && d.PayPeriodId == payPeriod.Id);
        if (duplicate)
            throw new InvalidOperationException("Já existe um documento para este funcionário neste período.");

        // Compute file hash
        using var stream = file.OpenReadStream();
        var hash = await SHA256.HashDataAsync(stream);
        var fileHash = Convert.ToHexString(hash).ToLowerInvariant();
        stream.Position = 0;

        // Generate S3 key
        var fileKey = $"{adminId}/{payPeriod.Year}/{payPeriod.Month:D2}/{employee.Id}/{Guid.NewGuid()}.pdf";

        // Upload to MinIO
        await _storage.UploadAsync(fileKey, stream, file.ContentType);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            PayPeriodId = payPeriod.Id,
            AdminId = adminId,
            OriginalFilename = file.FileName,
            OriginalFileKey = fileKey,
            OriginalFileHash = fileHash,
            FileSizeBytes = file.Length,
            Status = DocumentStatus.Uploaded,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        return new DocumentDto(
            document.Id,
            document.EmployeeId,
            employee.Name,
            document.PayPeriodId,
            payPeriod.Label ?? "",
            document.OriginalFilename,
            document.FileSizeBytes,
            document.Status.ToString(),
            null, null,
            document.CreatedAt,
            null
        );
    }

    public async Task DeleteAsync(Guid id, Guid adminId)
    {
        var document = await _db.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.AdminId == adminId && d.Status != DocumentStatus.Signed)
            ?? throw new InvalidOperationException("Documento não encontrado ou já foi assinado.");

        // Delete original file from MinIO
        try { await _storage.DeleteAsync(document.OriginalFileKey); } catch { /* ignore if already deleted */ }

        // Remove related notifications
        var notifications = await _db.Notifications.Where(n => n.DocumentId == id).ToListAsync();
        if (notifications.Any()) _db.Notifications.RemoveRange(notifications);

        // Remove related signing verifications
        var verifications = await _db.SigningVerifications.Where(v => v.DocumentId == id).ToListAsync();
        if (verifications.Any()) _db.SigningVerifications.RemoveRange(verifications);

        _db.Documents.Remove(document);
        await _db.SaveChangesAsync();
    }

    public async Task<(byte[] bytes, string filename)?> DownloadAsync(Guid id, Guid adminId, string type = "original")
    {
        var document = await _db.Documents
            .Include(d => d.Signature)
            .FirstOrDefaultAsync(d => d.Id == id && d.AdminId == adminId)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        string fileKey;
        string filename;

        if (type == "signed" && document.Status == DocumentStatus.Signed)
        {
            // If signed file doesn't exist yet, regenerate it on the fly
            if (string.IsNullOrEmpty(document.SignedFileKey))
            {
                await RegenerateSignedPdfAsync(document);
            }

            if (!string.IsNullOrEmpty(document.SignedFileKey))
            {
                fileKey = document.SignedFileKey;
                filename = $"assinado_{document.OriginalFilename}";
            }
            else
            {
                fileKey = document.OriginalFileKey;
                filename = document.OriginalFilename;
            }
        }
        else
        {
            fileKey = document.OriginalFileKey;
            filename = document.OriginalFilename;
        }

        try
        {
            var bytes = await _storage.DownloadAsync(fileKey);
            return (bytes, filename);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Regenerates the merged signed PDF (original + proof page) for documents
    /// that were signed before the merge feature was added.
    /// </summary>
    public async Task RegenerateSignedPdfAsync(Document document)
    {
        if (document.Signature == null)
        {
            document = await _db.Documents
                .Include(d => d.Signature)
                .Include(d => d.Employee)
                .FirstOrDefaultAsync(d => d.Id == document.Id)
                ?? throw new InvalidOperationException("Documento não encontrado.");
        }

        var sig = document.Signature;
        if (sig == null) return;

        // Load employee if not loaded
        if (document.Employee == null)
        {
            await _db.Entry(document).Reference(d => d.Employee).LoadAsync();
        }

        var periodLabel = await _db.PayPeriods
            .Where(p => p.Id == document.PayPeriodId)
            .Select(p => p.Label ?? $"{p.Month:D2}/{p.Year}")
            .FirstOrDefaultAsync() ?? "";

        var companyName = await _db.Admins
            .IgnoreQueryFilters()
            .Where(a => a.Id == document.AdminId)
            .Select(a => a.CompanyName)
            .FirstOrDefaultAsync() ?? "";

        // Download selfie photo from MinIO
        byte[] photoBytes;
        try
        {
            photoBytes = await _storage.DownloadAsync(sig.PhotoFileKey);
        }
        catch
        {
            return; // Can't regenerate without photo
        }

        var signedFileKey = $"{document.AdminId}/signed/{document.Id}/{Guid.NewGuid()}.pdf";

        var (pdfBytes, key) = await _pdfService.GenerateSignedPdfAsync(
            document.OriginalFileKey,
            signedFileKey,
            document.Employee!.Name,
            companyName,
            periodLabel,
            document.OriginalFilename,
            photoBytes,
            sig.PhotoMimeType,
            sig.PhotoHash,
            sig.SignerIp,
            sig.SignerUserAgent,
            sig.SignedAt,
            sig.ConsentText);

        // Delete old signed file if exists
        if (!string.IsNullOrEmpty(document.SignedFileKey))
        {
            try { await _storage.DeleteAsync(document.SignedFileKey); } catch { }
        }

        document.SignedFileKey = key;
        document.SignedFileHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(pdfBytes)).ToLowerInvariant();
        await _db.SaveChangesAsync();
    }

    /// <summary>Get raw Document entity for admin operations like regeneration.</summary>
    public async Task<Document?> GetDocumentEntityAsync(Guid id, Guid adminId)
    {
        return await _db.Documents
            .Include(d => d.Signature)
            .Include(d => d.Employee)
            .FirstOrDefaultAsync(d => d.Id == id && d.AdminId == adminId);
    }

    public async Task<List<object>> BatchUploadAsync(IFormFileCollection files, Guid[] employeeIds, int year, int month, Guid adminId)
    {
        if (files.Count != employeeIds.Length)
            throw new InvalidOperationException("Número de arquivos deve ser igual ao de funcionários.");

        var results = new List<object>();
        for (int i = 0; i < files.Count; i++)
        {
            try
            {
                var doc = await UploadAsync(files[i], employeeIds[i], year, month, adminId);
                results.Add(new { success = true, employeeId = employeeIds[i], document = doc });
            }
            catch (InvalidOperationException ex)
            {
                results.Add(new { success = false, employeeId = employeeIds[i], error = ex.Message });
            }
        }
        return results;
    }

    /// <summary>
    /// DOC-05: Replace/re-upload a PDF before it is signed.
    /// </summary>
    public async Task<DocumentDto> ReplaceFileAsync(Guid id, IFormFile file, Guid adminId)
    {
        var document = await _db.Documents
            .Include(d => d.Employee)
            .Include(d => d.PayPeriod)
            .FirstOrDefaultAsync(d => d.Id == id && d.AdminId == adminId)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        if (document.Status == DocumentStatus.Signed)
            throw new InvalidOperationException("Não é possível substituir um documento já assinado.");

        if (file.Length == 0)
            throw new InvalidOperationException("Arquivo vazio.");

        if (file.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("Arquivo excede o limite de 10 MB.");

        if (file.ContentType.ToLower() != "application/pdf")
            throw new InvalidOperationException("Apenas arquivos PDF são aceitos.");

        // Delete old file from storage
        try { await _storage.DeleteAsync(document.OriginalFileKey); } catch { }

        // Upload new file
        using var stream = file.OpenReadStream();
        var hash = await SHA256.HashDataAsync(stream);
        var fileHash = Convert.ToHexString(hash).ToLowerInvariant();
        stream.Position = 0;

        var newKey = $"{adminId}/{document.PayPeriod.Year}/{document.PayPeriod.Month:D2}/{document.EmployeeId}/{Guid.NewGuid()}.pdf";
        await _storage.UploadAsync(newKey, stream, file.ContentType);

        document.OriginalFileKey = newKey;
        document.OriginalFilename = file.FileName;
        document.OriginalFileHash = fileHash;
        document.FileSizeBytes = file.Length;
        document.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new DocumentDto(
            document.Id,
            document.EmployeeId,
            document.Employee.Name,
            document.PayPeriodId,
            document.PayPeriod.Label ?? "",
            document.OriginalFilename,
            document.FileSizeBytes,
            document.Status.ToString(),
            document.ViewedAt,
            document.LastNotifiedAt,
            document.CreatedAt,
            null
        );
    }
}
