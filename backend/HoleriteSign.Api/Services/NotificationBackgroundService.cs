using Hangfire;
using HoleriteSign.Api.DTOs;

namespace HoleriteSign.Api.Services;

/// <summary>
/// Hangfire-powered background notification service.
/// Wraps NotificationService methods for fire-and-forget execution.
/// </summary>
public class NotificationBackgroundService
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<NotificationBackgroundService> _logger;

    public NotificationBackgroundService(
        NotificationService notificationService,
        ILogger<NotificationBackgroundService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Enqueue a single notification to be sent in background.
    /// </summary>
    public static string EnqueueSend(Guid documentId, string channel, Guid adminId)
    {
        return BackgroundJob.Enqueue<NotificationBackgroundService>(
            svc => svc.SendNotificationAsync(documentId, channel, adminId));
    }

    /// <summary>
    /// Enqueue bulk notifications for a pay period.
    /// </summary>
    public static string EnqueueBulkSend(Guid payPeriodId, string channel, Guid adminId)
    {
        return BackgroundJob.Enqueue<NotificationBackgroundService>(
            svc => svc.SendBulkNotificationAsync(payPeriodId, channel, adminId));
    }

    /// <summary>
    /// Send a single notification (executed by Hangfire worker).
    /// </summary>
    [Queue("notifications")]
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 600 })]
    public async Task SendNotificationAsync(Guid documentId, string channel, Guid adminId)
    {
        _logger.LogInformation(
            "[Hangfire] Sending {Channel} notification for document {DocId}",
            channel, documentId);

        try
        {
            var result = await _notificationService.SendAsync(documentId, channel, adminId);
            _logger.LogInformation(
                "[Hangfire] Notification sent: {Status} for {Employee}",
                result.Status, result.EmployeeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Hangfire] Failed to send notification for document {DocId}",
                documentId);
            throw; // Re-throw for Hangfire retry
        }
    }

    /// <summary>
    /// Send bulk notifications for a period (executed by Hangfire worker).
    /// </summary>
    [Queue("notifications")]
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 300 })]
    public async Task SendBulkNotificationAsync(Guid payPeriodId, string channel, Guid adminId)
    {
        _logger.LogInformation(
            "[Hangfire] Sending bulk {Channel} notifications for period {PeriodId}",
            channel, payPeriodId);

        try
        {
            var results = await _notificationService.SendBulkAsync(payPeriodId, channel, adminId);
            var sent = results.Count(r => r.Status == "Sent");
            var failed = results.Count(r => r.Status == "Failed");
            _logger.LogInformation(
                "[Hangfire] Bulk send complete: {Sent} sent, {Failed} failed out of {Total}",
                sent, failed, results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Hangfire] Failed bulk send for period {PeriodId}",
                payPeriodId);
            throw;
        }
    }
}
