using HoleriteSign.Core.Enums;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

/// <summary>
/// Background job that runs every hour to mark documents with expired tokens as Expired.
/// </summary>
public class TokenExpirationJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TokenExpirationJob> _logger;

    public TokenExpirationJob(IServiceScopeFactory scopeFactory, ILogger<TokenExpirationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TokenExpirationJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireTokensAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TokenExpirationJob.");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ExpireTokensAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Find documents that have expired tokens and are still in Sent status
        var expiredDocs = await db.Documents
            .IgnoreQueryFilters() // Need to process all tenants
            .Where(d => d.TokenExpiresAt.HasValue
                        && d.TokenExpiresAt < now
                        && d.TokenUsedAt == null
                        && (d.Status == DocumentStatus.Sent || d.Status == DocumentStatus.Uploaded))
            .ToListAsync();

        if (expiredDocs.Count == 0) return;

        foreach (var doc in expiredDocs)
        {
            doc.Status = DocumentStatus.Expired;
            doc.UpdatedAt = now;
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Marked {Count} documents as expired.", expiredDocs.Count);
    }
}
