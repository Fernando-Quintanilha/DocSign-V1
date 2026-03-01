using System.Security.Claims;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;
    private readonly SigningService _signingService;

    public DocumentsController(DocumentService documentService, SigningService signingService)
    {
        _documentService = documentService;
        _signingService = signingService;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>GET /api/documents?employeeId=&payPeriodId=</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? employeeId, [FromQuery] Guid? payPeriodId)
    {
        var documents = await _documentService.ListAsync(GetAdminId(), employeeId, payPeriodId);
        return Ok(documents);
    }

    /// <summary>GET /api/documents/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id, GetAdminId());
        if (document is null) return NotFound(new { message = "Documento não encontrado." });
        return Ok(document);
    }

    /// <summary>
    /// POST /api/documents/upload
    /// Multipart form: file (PDF), employeeId, year, month
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(15 * 1024 * 1024)] // 15 MB
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] Guid employeeId,
        [FromForm] int year,
        [FromForm] int month)
    {
        try
        {
            var result = await _documentService.UploadAsync(file, employeeId, year, month, GetAdminId());
            return Created($"/api/documents/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/documents/{id}/generate-token — generates a signing link</summary>
    [HttpPost("{id:guid}/generate-token")]
    public async Task<IActionResult> GenerateToken(Guid id)
    {
        try
        {
            var result = await _signingService.GenerateTokenAsync(id, GetAdminId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>DELETE /api/documents/{id}</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _documentService.DeleteAsync(id, GetAdminId());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>GET /api/documents/{id}/download — Download original or signed PDF for admin</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, [FromQuery] string type = "original")
    {
        try
        {
            var result = await _documentService.DownloadAsync(id, GetAdminId(), type);
            if (result is null) return NotFound(new { message = "Arquivo não encontrado." });
            return File(result.Value.bytes, "application/pdf", result.Value.filename);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/documents/batch-upload — Upload multiple PDFs at once
    /// Form data: files[], employeeIds[], year, month
    /// </summary>
    [HttpPost("batch-upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB
    public async Task<IActionResult> BatchUpload(
        [FromForm] IFormFileCollection files,
        [FromForm] Guid[] employeeIds,
        [FromForm] int year,
        [FromForm] int month)
    {
        try
        {
            var results = await _documentService.BatchUploadAsync(files, employeeIds, year, month, GetAdminId());
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/documents/{id}/regenerate — Regenerate the merged signed PDF</summary>
    [HttpPost("{id:guid}/regenerate")]
    public async Task<IActionResult> Regenerate(Guid id)
    {
        try
        {
            var document = await _documentService.GetDocumentEntityAsync(id, GetAdminId());
            if (document == null) return NotFound(new { message = "Documento não encontrado." });

            await _documentService.RegenerateSignedPdfAsync(document);
            return Ok(new { message = "PDF assinado regenerado com sucesso." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>PUT /api/documents/{id}/replace — Replace PDF before signing (DOC-05)</summary>
    [HttpPut("{id:guid}/replace")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Replace(Guid id, IFormFile file)
    {
        try
        {
            var result = await _documentService.ReplaceFileAsync(id, file, GetAdminId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
