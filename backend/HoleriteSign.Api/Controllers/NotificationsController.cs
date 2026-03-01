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

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
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
}
