using System.Security.Claims;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;
    private readonly WhatsAppService _whatsAppService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        NotificationService notificationService,
        WhatsAppService whatsAppService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    private Guid GetAdminId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>POST /api/notifications/send</summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request)
    {
        try
        {
            var result = await _notificationService.SendAsync(request.DocumentId, request.Channel, GetAdminId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/notifications/send-bulk</summary>
    [HttpPost("send-bulk")]
    public async Task<IActionResult> SendBulk([FromBody] SendBulkNotificationRequest request)
    {
        try
        {
            var results = await _notificationService.SendBulkAsync(request.PayPeriodId, request.Channel, GetAdminId());
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>GET /api/notifications?documentId=</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? documentId)
    {
        var result = await _notificationService.ListAsync(documentId);
        return Ok(result);
    }

    // ── WhatsApp Management ────────────────────────────────

    /// <summary>POST /api/notifications/whatsapp/create-instance</summary>
    [HttpPost("whatsapp/create-instance")]
    public async Task<IActionResult> CreateWhatsAppInstance()
    {
        var (result, error, rawBody) = await _whatsAppService.CreateInstanceAsync();
        if (result == null)
        {
            _logger.LogWarning("WhatsApp create-instance failed: {Error}", error);
            return StatusCode(502, new { message = error ?? "Falha ao criar inst\u00e2ncia WhatsApp.", rawBody });
        }

        // Return a structured response that the frontend can easily use
        return Ok(new
        {
            instance = result.Instance,
            qrcode = result.Qrcode,
            rawBody, // Include for debugging
        });
    }

    /// <summary>GET /api/notifications/whatsapp/qrcode</summary>
    [HttpGet("whatsapp/qrcode")]
    public async Task<IActionResult> GetWhatsAppQrCode()
    {
        _logger.LogInformation("GetWhatsAppQrCode called");
        var result = await _whatsAppService.GetQrCodeAsync();
        if (result == null)
        {
            _logger.LogWarning("QR code result is null");
            return StatusCode(502, new { message = "Falha ao obter QR code. Verifique se a instância foi criada." });
        }
        _logger.LogInformation("QR code result: base64={HasBase64}, pairingCode={PairingCode}", 
            result.Base64 != null, result.PairingCode);
        return Ok(result);
    }

    /// <summary>GET /api/notifications/whatsapp/status</summary>
    [HttpGet("whatsapp/status")]
    public async Task<IActionResult> GetWhatsAppStatus()
    {
        var result = await _whatsAppService.GetConnectionStatusAsync();
        if (result == null) return Ok(new { state = "disconnected", instance = (object?)null });
        return Ok(result);
    }

    /// <summary>DELETE /api/notifications/whatsapp/logout</summary>
    [HttpDelete("whatsapp/logout")]
    public async Task<IActionResult> LogoutWhatsApp()
    {
        var ok = await _whatsAppService.LogoutInstanceAsync();
        return ok ? Ok(new { message = "WhatsApp desconectado." }) : StatusCode(502, new { message = "Falha ao desconectar." });
    }

    /// <summary>GET /api/notifications/whatsapp/diagnostic — Debug Evolution API connectivity</summary>
    [HttpGet("whatsapp/diagnostic")]
    public async Task<IActionResult> WhatsAppDiagnostic()
    {
        var diag = await _whatsAppService.RunDiagnosticAsync();
        return Ok(diag);
    }

    /// <summary>POST /api/notifications/webhook/evolution — Evolution API delivery webhooks (no auth)</summary>
    [HttpPost("webhook/evolution")]
    [AllowAnonymous]
    public IActionResult EvolutionWebhook([FromBody] EvolutionWebhookPayload payload)
    {
        _logger.LogInformation("Evolution webhook: {Event} for instance {Instance}", payload.Event, payload.Instance);

        // Process delivery/read acknowledgments
        switch (payload.Event)
        {
            case "messages.update":
                _logger.LogInformation("Message status update: {Data}", payload.Data?.ToString());
                break;
            case "connection.update":
                _logger.LogInformation("Connection update: {Data}", payload.Data?.ToString());
                break;
            default:
                _logger.LogDebug("Unhandled webhook event: {Event}", payload.Event);
                break;
        }

        return Ok(new { received = true });
    }
}
