using HoleriteSign.Api.DTOs;
using HoleriteSign.Core.Enums;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

public class DashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(Guid adminId)
    {
        // Explicit adminId filter — belt-and-suspenders with global query filter
        var totalEmployees = await _db.Employees
            .Where(e => e.AdminId == adminId && !e.DeletedAt.HasValue)
            .CountAsync();

        var activeEmployees = await _db.Employees
            .Where(e => e.AdminId == adminId && e.IsActive && !e.DeletedAt.HasValue)
            .CountAsync();

        var totalDocuments = await _db.Documents
            .Where(d => d.AdminId == adminId).CountAsync();

        var pendingDocuments = await _db.Documents
            .Where(d => d.AdminId == adminId &&
                (d.Status == DocumentStatus.Uploaded || d.Status == DocumentStatus.Sent))
            .CountAsync();

        var signedDocuments = await _db.Documents
            .Where(d => d.AdminId == adminId && d.Status == DocumentStatus.Signed)
            .CountAsync();

        var expiredDocuments = await _db.Documents
            .Where(d => d.AdminId == adminId && d.Status == DocumentStatus.Expired)
            .CountAsync();

        // Get plan info
        var admin = await _db.Admins
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == adminId)
            ?? throw new KeyNotFoundException($"Admin {adminId} não encontrado.");

        return new DashboardStatsDto(
            totalEmployees,
            activeEmployees,
            totalDocuments,
            pendingDocuments,
            signedDocuments,
            expiredDocuments,
            admin.Plan.DisplayName,
            admin.Plan.MaxEmployees,
            admin.Plan.MaxDocuments
        );
    }

    public async Task<EnhancedDashboardDto> GetEnhancedStatsAsync(Guid adminId)
    {
        var admin = await _db.Admins
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == adminId)
            ?? throw new KeyNotFoundException($"Admin {adminId} não encontrado.");

        // Basic counts — explicit adminId filtering
        var totalEmployees = await _db.Employees
            .Where(e => e.AdminId == adminId && !e.DeletedAt.HasValue).CountAsync();
        var activeEmployees = await _db.Employees
            .Where(e => e.AdminId == adminId && e.IsActive && !e.DeletedAt.HasValue).CountAsync();
        var totalDocuments = await _db.Documents
            .Where(d => d.AdminId == adminId).CountAsync();
        var pendingDocuments = await _db.Documents
            .Where(d => d.AdminId == adminId &&
                (d.Status == DocumentStatus.Uploaded || d.Status == DocumentStatus.Sent)).CountAsync();
        var signedDocuments = await _db.Documents
            .Where(d => d.AdminId == adminId && d.Status == DocumentStatus.Signed).CountAsync();
        var expiredDocuments = await _db.Documents
            .Where(d => d.AdminId == adminId && d.Status == DocumentStatus.Expired).CountAsync();

        // Documents used this month (PLAN-07: count sent/signed, not uploaded)
        var now = DateTime.UtcNow;
        var documentsUsedThisMonth = await _db.Documents
            .Where(d => d.AdminId == adminId && d.Status != DocumentStatus.Uploaded &&
                        d.CreatedAt.Year == now.Year && d.CreatedAt.Month == now.Month)
            .CountAsync();

        // Per-period breakdown
        var periods = await _db.PayPeriods
            .Where(p => p.AdminId == adminId)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Take(12)
            .Select(p => new PeriodSummaryDto(
                p.Id, p.Year, p.Month, p.Label ?? $"{p.Month:D2}/{p.Year}",
                p.Documents.Count,
                p.Documents.Count(d => d.Status == DocumentStatus.Signed),
                p.Documents.Count(d => d.Status == DocumentStatus.Uploaded || d.Status == DocumentStatus.Sent),
                p.Documents.Count(d => d.Status == DocumentStatus.Expired)
            ))
            .ToListAsync();

        // Pending employees — latest period for THIS admin
        var latestPeriod = await _db.PayPeriods
            .Where(p => p.AdminId == adminId)
            .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .FirstOrDefaultAsync();

        var pendingEmployees = new List<PendingEmployeeDto>();
        if (latestPeriod != null)
        {
            pendingEmployees = await GetPendingByPeriodAsync(latestPeriod.Id, adminId);
        }

        // Recent activity (last 20 events) — explicit admin filter
        var recentActivity = await _db.AuditLogs
            .Where(a => a.AdminId == adminId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .Select(a => new RecentActivityDto(
                a.Id, a.EventType, a.ActorType.ToString(),
                a.EmployeeId != null
                    ? _db.Employees.IgnoreQueryFilters().Where(e => e.Id == a.EmployeeId).Select(e => e.Name).FirstOrDefault()
                    : null,
                a.DocumentId != null
                    ? _db.Documents.IgnoreQueryFilters().Where(d => d.Id == a.DocumentId).Select(d => d.OriginalFilename).FirstOrDefault()
                    : null,
                a.CreatedAt
            ))
            .ToListAsync();

        return new EnhancedDashboardDto(
            totalEmployees, activeEmployees, totalDocuments,
            pendingDocuments, signedDocuments, expiredDocuments,
            admin.Plan.DisplayName, admin.Plan.MaxEmployees, admin.Plan.MaxDocuments,
            documentsUsedThisMonth,
            periods, pendingEmployees, recentActivity
        );
    }

    public async Task<List<PendingEmployeeDto>> GetPendingByPeriodAsync(Guid payPeriodId, Guid? adminId = null)
    {
        var docsQuery = _db.Documents
            .Include(d => d.Employee)
            .Where(d => d.PayPeriodId == payPeriodId);
        if (adminId.HasValue)
            docsQuery = docsQuery.Where(d => d.AdminId == adminId.Value);

        var employeesWithPendingDocs = await docsQuery
            .Where(d => d.Status != DocumentStatus.Signed)
            .Select(d => new PendingEmployeeDto(
                d.EmployeeId, d.Employee.Name, d.Employee.Email, d.Employee.WhatsApp,
                d.Id, d.Status.ToString(), d.LastNotifiedAt
            ))
            .ToListAsync();

        var employeeIdsWithDocs = await docsQuery
            .Select(d => d.EmployeeId)
            .ToListAsync();

        var empQuery = _db.Employees
            .Where(e => e.IsActive && !e.DeletedAt.HasValue && !employeeIdsWithDocs.Contains(e.Id));
        if (adminId.HasValue)
            empQuery = empQuery.Where(e => e.AdminId == adminId.Value);

        var employeesWithoutDocs = await empQuery
            .Select(e => new PendingEmployeeDto(
                e.Id, e.Name, e.Email, e.WhatsApp,
                null, "NoDocument", null
            ))
            .ToListAsync();

        return employeesWithPendingDocs.Concat(employeesWithoutDocs).ToList();
    }
}
