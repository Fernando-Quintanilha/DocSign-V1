using System.Security.Claims;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>GET /api/dashboard/stats</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _dashboardService.GetStatsAsync(GetAdminId());
        return Ok(stats);
    }

    /// <summary>GET /api/dashboard/enhanced — Full dashboard with periods, pending, activity</summary>
    [HttpGet("enhanced")]
    public async Task<IActionResult> GetEnhancedStats()
    {
        var adminId = GetAdminId();
        var stats = await _dashboardService.GetEnhancedStatsAsync(adminId);
        return Ok(stats);
    }

    /// <summary>GET /api/dashboard/pending?payPeriodId= — Who hasn't signed yet</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] Guid payPeriodId)
    {
        var pending = await _dashboardService.GetPendingByPeriodAsync(payPeriodId, GetAdminId());
        return Ok(pending);
    }
}
