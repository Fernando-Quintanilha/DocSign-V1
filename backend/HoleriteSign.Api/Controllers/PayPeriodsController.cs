using System.Security.Claims;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayPeriodsController : ControllerBase
{
    private readonly PayPeriodService _payPeriodService;

    public PayPeriodsController(PayPeriodService payPeriodService)
    {
        _payPeriodService = payPeriodService;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>GET /api/payperiods</summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var periods = await _payPeriodService.ListAsync(GetAdminId());
        return Ok(periods);
    }

    /// <summary>GET /api/payperiods/{id} — period details with employee statuses</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _payPeriodService.GetByIdAsync(id, GetAdminId());
        if (result == null) return NotFound(new { message = "Período não encontrado." });
        return Ok(result);
    }

    /// <summary>POST /api/payperiods</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePayPeriodRequest request)
    {
        try
        {
            var result = await _payPeriodService.CreateAsync(request, GetAdminId());
            return Created($"/api/payperiods/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
