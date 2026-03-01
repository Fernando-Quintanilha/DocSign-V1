using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Core.Interfaces;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

public class AuditLogService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        string eventType,
        ActorType actorType,
        Guid? adminId = null,
        Guid? employeeId = null,
        Guid? documentId = null,
        string? eventData = null,
        string? actorIp = null,
        string? actorUserAgent = null)
    {
        // Get previous hash in chain (scoped per document or global)
        string? prevHash = null;
        if (documentId.HasValue)
        {
            prevHash = await _db.AuditLogs
                .Where(a => a.DocumentId == documentId.Value)
                .OrderByDescending(a => a.Id)
                .Select(a => a.EntryHash)
                .FirstOrDefaultAsync();
        }
        else
        {
            prevHash = await _db.AuditLogs
                .OrderByDescending(a => a.Id)
                .Select(a => a.EntryHash)
                .FirstOrDefaultAsync();
        }

        var entry = new AuditLog
        {
            AdminId = adminId,
            EmployeeId = employeeId,
            DocumentId = documentId,
            EventType = eventType,
            EventData = eventData,
            ActorType = actorType,
            ActorIp = actorIp,
            ActorUserAgent = actorUserAgent,
            PrevHash = prevHash,
            ChainVersion = 1,
            CreatedAt = DateTime.UtcNow,
        };

        // Compute entry hash (SEC-16)
        entry.EntryHash = ComputeEntryHash(entry);

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetLogsAsync(
        Guid? adminId = null,
        Guid? documentId = null,
        Guid? employeeId = null,
        string? eventType = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (adminId.HasValue)
            query = query.Where(a => a.AdminId == adminId.Value);
        if (documentId.HasValue)
            query = query.Where(a => a.DocumentId == documentId.Value);
        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(a => a.EventType == eventType);

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync(
        Guid? adminId = null,
        Guid? documentId = null,
        Guid? employeeId = null,
        string? eventType = null)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (adminId.HasValue)
            query = query.Where(a => a.AdminId == adminId.Value);
        if (documentId.HasValue)
            query = query.Where(a => a.DocumentId == documentId.Value);
        if (employeeId.HasValue)
            query = query.Where(a => a.EmployeeId == employeeId.Value);
        if (!string.IsNullOrEmpty(eventType))
            query = query.Where(a => a.EventType == eventType);

        return await query.CountAsync();
    }

    private static string ComputeEntryHash(AuditLog entry)
    {
        var payload = JsonSerializer.Serialize(new
        {
            entry.AdminId,
            entry.EmployeeId,
            entry.DocumentId,
            entry.EventType,
            entry.EventData,
            entry.ActorType,
            entry.ActorIp,
            entry.PrevHash,
            entry.CreatedAt,
        });

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
