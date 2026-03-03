using System.Security.Claims;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>POST /api/notifications/send-bulk-async — Fire-and-forget via Hangfire</summary>
    [HttpPost("send-bulk-async")]
    public IActionResult SendBulkAsync([FromBody] SendBulkNotificationRequest request)
    {
        var jobId = NotificationBackgroundService.EnqueueBulkSend(
            request.PayPeriodId, request.Channel, GetAdminId());
        return Accepted(new { message = "Envio em lote agendado com sucesso.", jobId });
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
    public async Task<IActionResult> EvolutionWebhook([FromBody] EvolutionWebhookPayload payload)
    {
        _logger.LogInformation("Evolution webhook: {Event} for instance {Instance}", payload.Event, payload.Instance);

        // Process delivery/read acknowledgments
        switch (payload.Event)
        {
            case "messages.update":
                await ProcessMessageStatusUpdate(payload.Data);
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

    /// <summary>
    /// Process Evolution API message status updates (delivered, read, etc.)
    /// Evolution sends arrays of status updates for messages.
    /// </summary>
    private async Task ProcessMessageStatusUpdate(System.Text.Json.JsonElement? data)
    {
        if (data == null) return;

        try
        {
            // Evolution API sends an array of status updates
            // Each has: { keyId, status, ... }
            // Status can be: "DELIVERY_ACK" (delivered), "READ" (read), "PLAYED" (played)
            var updates = new List<(string messageId, string status)>();

            if (data.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in data.Value.EnumerateArray())
                {
                    var keyId = item.TryGetProperty("keyId", out var kid) ? kid.GetString() :
                                item.TryGetProperty("key", out var key) && key.TryGetProperty("id", out var id) ? id.GetString() : null;
                    var status = item.TryGetProperty("status", out var s) ? s.GetString() : null;
                    if (keyId != null && status != null)
                        updates.Add((keyId, status));
                }
            }
            else if (data.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                var keyId = data.Value.TryGetProperty("keyId", out var kid) ? kid.GetString() :
                            data.Value.TryGetProperty("key", out var key) && key.TryGetProperty("id", out var id) ? id.GetString() : null;
                var status = data.Value.TryGetProperty("status", out var s) ? s.GetString() : null;
                if (keyId != null && status != null)
                    updates.Add((keyId, status));
            }

            if (updates.Count == 0)
            {
                _logger.LogDebug("No parseable status updates in webhook data");
                return;
            }

            var messageIds = updates.Select(u => u.messageId).ToList();

            using var scope = HttpContext.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HoleriteSign.Infrastructure.Data.AppDbContext>();

            var notifications = await db.Notifications
                .Where(n => n.ExternalId != null && messageIds.Contains(n.ExternalId))
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var notif in notifications)
            {
                var update = updates.First(u => u.messageId == notif.ExternalId);
                switch (update.status.ToUpperInvariant())
                {
                    case "DELIVERY_ACK":
                    case "DELIVERED":
                    case "SERVER_ACK":
                        if (notif.DeliveredAt == null)
                        {
                            notif.DeliveredAt = now;
                            notif.Status = HoleriteSign.Core.Enums.NotificationStatus.Delivered;
                            _logger.LogInformation("Notification {Id} marked as delivered (msg: {MsgId})", notif.Id, notif.ExternalId);
                        }
                        break;
                    case "READ":
                    case "PLAYED":
                        notif.DeliveredAt ??= now;
                        notif.ReadAt = now;
                        notif.Status = HoleriteSign.Core.Enums.NotificationStatus.Read;
                        _logger.LogInformation("Notification {Id} marked as read (msg: {MsgId})", notif.Id, notif.ExternalId);
                        break;
                }
            }

            if (notifications.Count > 0)
                await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error processing message status update webhook");
        }
    }
}
