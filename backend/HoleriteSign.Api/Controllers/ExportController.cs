using System.Security.Claims;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly ExportService _exportService;

    public ExportController(ExportService exportService)
    {
        _exportService = exportService;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>GET /api/export/period/{id} — Download all signed PDFs for a month as ZIP</summary>
    [HttpGet("period/{payPeriodId}")]
    public async Task<IActionResult> ExportPeriod(Guid payPeriodId)
    {
        var result = await _exportService.ExportSignedPdfsByPeriodAsync(payPeriodId, GetAdminId());
        if (result == null)
            return NotFound(new { error = "Nenhum documento assinado encontrado para este período." });

        return File(result.Value.ZipBytes, "application/zip", result.Value.FileName);
    }

    /// <summary>GET /api/export/employees — Export employee list as CSV</summary>
    [HttpGet("employees")]
    public async Task<IActionResult> ExportEmployees()
    {
        var csv = await _exportService.ExportEmployeesAsCsvAsync(GetAdminId());
        return File(csv, "text/csv; charset=utf-8", "funcionarios.csv");
    }

    /// <summary>GET /api/export/audit-logs — Export audit logs as CSV</summary>
    [HttpGet("audit-logs")]
    public async Task<IActionResult> ExportAuditLogs()
    {
        var csv = await _exportService.ExportAuditLogsAsCsvAsync(GetAdminId());
        return File(csv, "text/csv; charset=utf-8", "audit_logs.csv");
    }

    /// <summary>GET /api/export/all — Export ALL data as ZIP (BAK-02)</summary>
    [HttpGet("all")]
    public async Task<IActionResult> ExportAll()
    {
        var result = await _exportService.ExportAllDataAsync(GetAdminId());
        return File(result.ZipBytes, "application/zip", result.FileName);
    }
}
