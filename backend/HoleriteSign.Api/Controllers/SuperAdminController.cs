using System.Security.Claims;
using HoleriteSign.Core.Enums;
using HoleriteSign.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Controllers;

/// <summary>
/// Super Admin endpoints — manage accounts, plans, global metrics.
/// Requires SuperAdmin role.
/// </summary>
[ApiController]
[Route("api/super")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public SuperAdminController(AppDbContext db)
    {
        _db = db;
    }

    // ── Accounts ──────────────────────────────────────────

    /// <summary>GET /api/super/accounts — list all admin accounts</summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> ListAccounts(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Admins
            .Include(a => a.Plan)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(term) ||
                a.Email.ToLower().Contains(term) ||
                a.CompanyName.ToLower().Contains(term));
        }

        var total = await query.CountAsync();

        var accounts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Email,
                a.CompanyName,
                Role = a.Role.ToString(),
                PlanName = a.Plan.DisplayName,
                PlanId = a.PlanId,
                a.EmailVerified,
                a.IsActive,
                a.CreatedAt,
                EmployeeCount = a.Employees.Count(e => !e.DeletedAt.HasValue),
                DocumentCount = a.Documents.Count(),
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, accounts });
    }

    /// <summary>GET /api/super/accounts/{id} — get account details</summary>
    [HttpGet("accounts/{id:guid}")]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        var account = await _db.Admins
            .Include(a => a.Plan)
            .Where(a => a.Id == id)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Email,
                a.CompanyName,
                Role = a.Role.ToString(),
                PlanName = a.Plan.DisplayName,
                PlanId = a.PlanId,
                a.EmailVerified,
                a.IsActive,
                a.CreatedAt,
                a.UpdatedAt,
                EmployeeCount = a.Employees.Count(e => !e.DeletedAt.HasValue),
                DocumentCount = a.Documents.Count(),
                SignedCount = a.Documents.Count(d => d.Status == DocumentStatus.Signed),
            })
            .FirstOrDefaultAsync();

        if (account is null) return NotFound(new { message = "Conta não encontrada." });
        return Ok(account);
    }

    /// <summary>PUT /api/super/accounts/{id} — update account (plan, active status)</summary>
    [HttpPut("accounts/{id:guid}")]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateAccountRequest request)
    {
        var admin = await _db.Admins.FindAsync(id);
        if (admin is null) return NotFound(new { message = "Conta não encontrada." });

        if (request.PlanId.HasValue)
        {
            var plan = await _db.Plans.FindAsync(request.PlanId.Value);
            if (plan is null) return BadRequest(new { message = "Plano não encontrado." });
            admin.PlanId = request.PlanId.Value;
        }

        if (request.IsActive.HasValue)
            admin.IsActive = request.IsActive.Value;

        if (request.Role is not null)
        {
            if (!Enum.TryParse<AdminRole>(request.Role, true, out var role))
                return BadRequest(new { message = "Role inválida. Use 'Admin' ou 'SuperAdmin'." });
            admin.Role = role;
        }

        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Conta atualizada com sucesso." });
    }

    // ── Global Metrics ────────────────────────────────────

    /// <summary>GET /api/super/metrics — global platform metrics</summary>
    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var totalAccounts = await _db.Admins.CountAsync();
        var activeAccounts = await _db.Admins.CountAsync(a => a.IsActive);
        var totalEmployees = await _db.Employees.CountAsync(e => !e.DeletedAt.HasValue);
        var totalDocuments = await _db.Documents.IgnoreQueryFilters().CountAsync();
        var signedDocuments = await _db.Documents.IgnoreQueryFilters()
            .CountAsync(d => d.Status == DocumentStatus.Signed);
        var pendingDocuments = await _db.Documents.IgnoreQueryFilters()
            .CountAsync(d => d.Status == DocumentStatus.Uploaded || d.Status == DocumentStatus.Sent);

        // Documents this month
        var now = DateTime.UtcNow;
        var docsThisMonth = await _db.Documents.IgnoreQueryFilters()
            .CountAsync(d => d.CreatedAt.Year == now.Year && d.CreatedAt.Month == now.Month);

        // Plan distribution
        var planDistribution = await _db.Admins
            .Include(a => a.Plan)
            .GroupBy(a => a.Plan.DisplayName)
            .Select(g => new { Plan = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            totalAccounts,
            activeAccounts,
            totalEmployees,
            totalDocuments,
            signedDocuments,
            pendingDocuments,
            docsThisMonth,
            planDistribution,
        });
    }

    // ── Plans Management ──────────────────────────────────

    /// <summary>GET /api/super/plans — list all plans (including inactive)</summary>
    [HttpGet("plans")]
    public async Task<IActionResult> ListPlans()
    {
        var plans = await _db.Plans
            .OrderBy(p => p.PriceMonthly)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.DisplayName,
                p.MaxDocuments,
                p.MaxEmployees,
                p.PriceMonthly,
                p.IsActive,
                AdminCount = p.Admins.Count(),
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>PUT /api/super/plans/{id} — update a plan</summary>
    [HttpPut("plans/{id:guid}")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanRequest request)
    {
        var plan = await _db.Plans.FindAsync(id);
        if (plan is null) return NotFound(new { message = "Plano não encontrado." });

        if (request.DisplayName is not null) plan.DisplayName = request.DisplayName;
        if (request.MaxDocuments.HasValue) plan.MaxDocuments = request.MaxDocuments.Value;
        if (request.MaxEmployees.HasValue) plan.MaxEmployees = request.MaxEmployees.Value;
        if (request.PriceMonthly.HasValue) plan.PriceMonthly = request.PriceMonthly.Value;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Plano atualizado com sucesso." });
    }

    // ── Audit Logs (Global) ───────────────────────────────

    /// <summary>GET /api/super/audit-logs — global audit log view</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> ListAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _db.AuditLogs.AsQueryable();

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id,
                l.EventType,
                ActorType = l.ActorType.ToString(),
                l.ActorIp,
                l.AdminId,
                l.EmployeeId,
                l.DocumentId,
                l.EventData,
                l.CreatedAt,
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, logs });
    }
}

// ── Request DTOs ──────────────────────────────────────────

public record UpdateAccountRequest(
    Guid? PlanId = null,
    bool? IsActive = null,
    string? Role = null
);

public record UpdatePlanRequest(
    string? DisplayName = null,
    int? MaxDocuments = null,
    int? MaxEmployees = null,
    decimal? PriceMonthly = null,
    bool? IsActive = null
);
