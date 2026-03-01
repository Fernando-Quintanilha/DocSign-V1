using System.Security.Claims;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _employeeService;

    public EmployeesController(EmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>GET /api/employees?search=</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search)
    {
        var employees = await _employeeService.ListAsync(GetAdminId(), search);
        return Ok(employees);
    }

    /// <summary>GET /api/employees/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = await _employeeService.GetByIdAsync(id, GetAdminId());
        if (employee is null) return NotFound(new { message = "Funcionário não encontrado." });
        return Ok(employee);
    }

    /// <summary>POST /api/employees</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.CreateAsync(request, GetAdminId());
            return Created($"/api/employees/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PUT /api/employees/{id}</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.UpdateAsync(id, request, GetAdminId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>DELETE /api/employees/{id}</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _employeeService.DeleteAsync(id, GetAdminId());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/employees/import — Import employees from CSV</summary>
    [HttpPost("import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "Arquivo vazio." });

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".csv")
            return BadRequest(new { message = "Apenas arquivos .csv são aceitos." });

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _employeeService.ImportCsvAsync(stream, GetAdminId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
