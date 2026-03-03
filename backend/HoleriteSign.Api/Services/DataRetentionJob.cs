using HoleriteSign.Core.Enums;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

/// <summary>
/// Background job that runs daily to clean up expired data per the retention policy:
///   - Clean expired signing tokens (older than 30 days) → reset token fields
///   - Archive old notifications (older than 12 months) → delete notification records
///   - Anonymize soft-deleted employees (deleted > 5 years) → LGPD compliance
///   - Clean orphaned storage files (future)
/// </summary>
public class DataRetentionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataRetentionJob> _logger;

    public DataRetentionJob(IServiceScopeFactory scopeFactory, ILogger<DataRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DataRetentionJob started.");

        // Wait 5 minutes after startup to not compete with migrations
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRetentionTasksAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DataRetentionJob.");
            }

            // Run once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunRetentionTasksAsync()
    {
        _logger.LogInformation("DataRetentionJob: Starting retention tasks...");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var totalCleaned = 0;

        // 1. Clean expired signing tokens older than 30 days
        totalCleaned += await CleanExpiredTokensAsync(db);

        // 2. Archive old failed notifications older than 12 months
        totalCleaned += await ArchiveOldNotificationsAsync(db);

        // 3. Anonymize soft-deleted employees older than 5 years (LGPD)
        totalCleaned += await AnonymizeDeletedEmployeesAsync(db);

        if (totalCleaned > 0)
        {
            _logger.LogInformation("DataRetentionJob: Cleaned {Count} total records.", totalCleaned);
        }
        else
        {
            _logger.LogDebug("DataRetentionJob: No records to clean.");
        }
    }

    /// <summary>
    /// Reset token fields on documents where the token expired more than 30 days ago
    /// and the document was already marked as expired.
    /// This prevents accumulation of stale token data.
    /// </summary>
    private async Task<int> CleanExpiredTokensAsync(AppDbContext db)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var staleTokenDocs = await db.Documents
            .IgnoreQueryFilters()
            .Where(d => d.Status == DocumentStatus.Expired
                        && d.TokenExpiresAt.HasValue
                        && d.TokenExpiresAt < cutoff
                        && d.SigningTokenHash != null)
            .ToListAsync();

        foreach (var doc in staleTokenDocs)
        {
            doc.SigningTokenHash = null;
            doc.TokenExpiresAt = null;
            doc.UpdatedAt = DateTime.UtcNow;
        }

        if (staleTokenDocs.Count > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("DataRetentionJob: Cleaned {Count} expired token(s).", staleTokenDocs.Count);
        }

        return staleTokenDocs.Count;
    }

    /// <summary>
    /// Delete notification records that are older than 12 months and have status Failed.
    /// Sent/Delivered notifications are kept for audit purposes (linked to audit logs).
    /// </summary>
    private async Task<int> ArchiveOldNotificationsAsync(AppDbContext db)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-12);

        var oldNotifications = await db.Notifications
            .IgnoreQueryFilters()
            .Where(n => n.CreatedAt < cutoff
                        && n.Status == NotificationStatus.Failed)
            .ToListAsync();

        if (oldNotifications.Count > 0)
        {
            db.Notifications.RemoveRange(oldNotifications);
            await db.SaveChangesAsync();
            _logger.LogInformation("DataRetentionJob: Archived {Count} old failed notification(s).", oldNotifications.Count);
        }

        return oldNotifications.Count;
    }

    /// <summary>
    /// Anonymize employee records that were soft-deleted more than 5 years ago.
    /// Per Brazilian labor law (CLT), payslip records must be retained for 5 years.
    /// After that, LGPD requires anonymization/deletion of personal data.
    /// </summary>
    private async Task<int> AnonymizeDeletedEmployeesAsync(AppDbContext db)
    {
        var cutoff = DateTime.UtcNow.AddYears(-5);

        var oldEmployees = await db.Employees
            .IgnoreQueryFilters()
            .Where(e => e.DeletedAt.HasValue
                        && e.DeletedAt < cutoff
                        && e.Name != "ANONIMIZADO") // Not already anonymized
            .ToListAsync();

        foreach (var emp in oldEmployees)
        {
            emp.Name = "ANONIMIZADO";
            emp.Email = null;
            emp.WhatsApp = null;
            emp.CpfEncrypted = null;
            emp.CpfLast4 = null;
            emp.BirthDateEncrypted = null;
            emp.UpdatedAt = DateTime.UtcNow;
        }

        if (oldEmployees.Count > 0)
        {
            await db.SaveChangesAsync();
            _logger.LogInformation("DataRetentionJob: Anonymized {Count} old employee(s) per LGPD.", oldEmployees.Count);
        }

        return oldEmployees.Count;
    }
}
