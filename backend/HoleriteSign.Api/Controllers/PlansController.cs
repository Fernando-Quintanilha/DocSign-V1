using System.Security.Claims;
using HoleriteSign.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlansController : ControllerBase
{
    private readonly AppDbContext _db;

    public PlansController(AppDbContext db)
    {
        _db = db;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>GET /api/plans — list all active plans</summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var plans = await _db.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.PriceMonthly)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.DisplayName,
                p.MaxDocuments,
                p.MaxEmployees,
                p.PriceMonthly,
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>GET /api/plans/current — current admin plan info</summary>
    [HttpGet("current")]
    public async Task<IActionResult> Current()
    {
        var admin = await _db.Admins
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == GetAdminId());
        if (admin == null) return NotFound("Admin não encontrado.");

        return Ok(new
        {
            admin.Plan.Id,
            admin.Plan.Name,
            admin.Plan.DisplayName,
            admin.Plan.MaxDocuments,
            admin.Plan.MaxEmployees,
            admin.Plan.PriceMonthly,
        });
    }
}
