using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace HoleriteSign.Api.Controllers;

/// <summary>
/// Public controller — no [Authorize]. Handles the employee signing flow.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SigningController : ControllerBase
{
    private readonly SigningService _signingService;

    public SigningController(SigningService signingService)
    {
        _signingService = signingService;
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private string GetUserAgent() =>
        Request.Headers.UserAgent.ToString();

    /// <summary>GET /api/signing/validate/{token}</summary>
    [HttpGet("validate/{token}")]
    public async Task<IActionResult> ValidateToken(string token)
    {
        var result = await _signingService.ValidateTokenAsync(token);
        if (result is null)
            return NotFound(new { message = "Token inválido ou expirado." });
        return Ok(result);
    }

    /// <summary>POST /api/signing/verify/{token}</summary>
    [HttpPost("verify/{token}")]
    public async Task<IActionResult> VerifyIdentity(string token, [FromBody] VerifyIdentityRequest request)
    {
        var result = await _signingService.VerifyIdentityAsync(token, request, GetIp(), GetUserAgent());
        if (!result.Verified)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>GET /api/signing/document/{token}</summary>
    [HttpGet("document/{token}")]
    public async Task<IActionResult> GetDocument(string token)
    {
        var result = await _signingService.GetDocumentForSigningAsync(token);
        if (result is null)
            return NotFound(new { message = "Documento não disponível. Verifique sua identidade primeiro." });
        return Ok(result);
    }

    /// <summary>GET /api/signing/download/{token} — serves the actual PDF inline (for iframe viewing)</summary>
    [HttpGet("download/{token}")]
    public async Task<IActionResult> DownloadFile(string token)
    {
        var result = await _signingService.GetFileForDownloadAsync(token);
        if (result is null)
            return NotFound(new { message = "Arquivo não encontrado." });

        // Return without filename so Content-Disposition defaults to "inline"
        // This allows the PDF to render inside an iframe on the signing page
        return File(result.Value.bytes, "application/pdf");
    }

    /// <summary>POST /api/signing/sign/{token}</summary>
    [HttpPost("sign/{token}")]
    public async Task<IActionResult> Sign(string token, [FromBody] SignDocumentRequest request)
    {
        var result = await _signingService.SignAsync(token, request, GetIp(), GetUserAgent());
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
