using System.IO.Compression;
using System.Text;
using HoleriteSign.Core.Enums;
using HoleriteSign.Core.Interfaces;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

public class ExportService
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public ExportService(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>
    /// BAK-01: Export all signed PDFs for a pay period as a ZIP file.
    /// </summary>
    public async Task<(byte[] ZipBytes, string FileName)?> ExportSignedPdfsByPeriodAsync(Guid payPeriodId, Guid adminId)
    {
        var period = await _db.PayPeriods.FirstOrDefaultAsync(p => p.Id == payPeriodId && p.AdminId == adminId);
        if (period == null) return null;

        var signedDocs = await _db.Documents
            .Include(d => d.Employee)
            .Where(d => d.AdminId == adminId && d.PayPeriodId == payPeriodId && d.Status == DocumentStatus.Signed)
            .ToListAsync();

        if (signedDocs.Count == 0) return null;

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var doc in signedDocs)
            {
                // Try signed file first, then original
                var fileKey = doc.SignedFileKey ?? doc.OriginalFileKey;

                byte[]? fileBytes = null;
                try
                {
                    fileBytes = await _storage.DownloadAsync(fileKey);
                }
                catch
                {
                    // Fallback to original if signed not found
                    try { fileBytes = await _storage.DownloadAsync(doc.OriginalFileKey); } catch { }
                }

                if (fileBytes != null)
                {
                    var entryName = $"{doc.Employee.Name.Replace("/", "_")}_{doc.OriginalFilename}";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(fileBytes);
                }
            }
        }

        memoryStream.Position = 0;
        var zipFileName = $"holerites_{period.Label ?? $"{period.Year}-{period.Month:D2}"}_assinados.zip";
        return (memoryStream.ToArray(), zipFileName);
    }

    /// <summary>
    /// BAK-03: Export employee list as CSV.
    /// </summary>
    public async Task<byte[]> ExportEmployeesAsCsvAsync(Guid adminId)
    {
        var employees = await _db.Employees
            .Where(e => e.AdminId == adminId && !e.DeletedAt.HasValue)
            .OrderBy(e => e.Name)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Nome,Email,WhatsApp,CPF (últimos 4),Ativo,Criado em");

        foreach (var e in employees)
        {
            var name = EscapeCsv(e.Name);
            var email = EscapeCsv(e.Email ?? "");
            var whatsapp = EscapeCsv(e.WhatsApp ?? "");
            var cpf = EscapeCsv(e.CpfLast4 ?? "");
            var active = e.IsActive ? "Sim" : "Não";
            var created = e.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            sb.AppendLine($"{name},{email},{whatsapp},{cpf},{active},{created}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    /// <summary>
    /// BAK-04: Export audit logs as CSV.
    /// </summary>
    public async Task<byte[]> ExportAuditLogsAsCsvAsync(Guid adminId)
    {
        var logs = await _db.AuditLogs
            .Where(a => a.AdminId == adminId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10000) // Limit to avoid overload
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Evento,Tipo Ator,IP,Admin ID,Funcionário ID,Documento ID,Data,Dados");

        foreach (var log in logs)
        {
            sb.AppendLine(string.Join(",",
                log.Id,
                EscapeCsv(log.EventType),
                EscapeCsv(log.ActorType.ToString()),
                EscapeCsv(log.ActorIp ?? ""),
                log.AdminId?.ToString() ?? "",
                log.EmployeeId?.ToString() ?? "",
                log.DocumentId?.ToString() ?? "",
                log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                EscapeCsv(log.EventData ?? "")
            ));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    /// <summary>
    /// BAK-02: Export ALL data as a single ZIP (all periods, employees CSV, audit CSV).
    /// </summary>
    public async Task<(byte[] ZipBytes, string FileName)> ExportAllDataAsync(Guid adminId)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // 1. Employees CSV
            var employeesCsv = await ExportEmployeesAsCsvAsync(adminId);
            var empEntry = archive.CreateEntry("funcionarios.csv", CompressionLevel.Optimal);
            using (var empStream = empEntry.Open()) await empStream.WriteAsync(employeesCsv);

            // 2. Audit logs CSV
            var auditCsv = await ExportAuditLogsAsCsvAsync(adminId);
            var auditEntry = archive.CreateEntry("auditoria.csv", CompressionLevel.Optimal);
            using (var auditStream = auditEntry.Open()) await auditStream.WriteAsync(auditCsv);

            // 3. All signed PDFs organized by period
            var periods = await _db.PayPeriods
                .Where(p => p.AdminId == adminId)
                .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
                .ToListAsync();

            foreach (var period in periods)
            {
                var docs = await _db.Documents
                    .Include(d => d.Employee)
                    .Where(d => d.AdminId == adminId && d.PayPeriodId == period.Id)
                    .ToListAsync();

                var folderName = period.Label ?? $"{period.Year}-{period.Month:D2}";

                foreach (var doc in docs)
                {
                    var fileKey = doc.SignedFileKey ?? doc.OriginalFileKey;
                    byte[]? fileBytes = null;
                    try { fileBytes = await _storage.DownloadAsync(fileKey); } catch { }
                    if (fileBytes == null) try { fileBytes = await _storage.DownloadAsync(doc.OriginalFileKey); } catch { }

                    if (fileBytes != null)
                    {
                        var empName = doc.Employee?.Name?.Replace("/", "_").Replace("\\", "_") ?? doc.EmployeeId.ToString();
                        var entryName = $"{folderName}/{empName}_{doc.OriginalFilename}";
                        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(fileBytes);
                    }
                }
            }
        }

        memoryStream.Position = 0;
        var fileName = $"holeritesign_export_completo_{DateTime.UtcNow:yyyy-MM-dd}.zip";
        return (memoryStream.ToArray(), fileName);
    }
}
