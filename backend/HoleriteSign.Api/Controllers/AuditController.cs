using System.Security.Claims;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly AuditLogService _auditService;

    public AuditController(AuditLogService auditService)
    {
        _auditService = auditService;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>GET /api/audit?documentId=&employeeId=&eventType=&page=1&pageSize=50</summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid? documentId,
        [FromQuery] Guid? employeeId,
        [FromQuery] string? eventType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var adminId = GetAdminId();
        var logs = await _auditService.GetLogsAsync(adminId, documentId, employeeId, eventType, page, pageSize);
        var total = await _auditService.CountAsync(adminId, documentId, employeeId, eventType);

        var dtos = logs.Select(l => new AuditLogDto(
            l.Id,
            l.EventType,
            l.ActorType.ToString(),
            l.ActorIp,
            l.AdminId,
            l.EmployeeId,
            l.DocumentId,
            l.EventData,
            l.CreatedAt
        )).ToList();

        return Ok(new
        {
            data = dtos,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)total / pageSize),
        });
    }
}
