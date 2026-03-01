using HoleriteSign.Core.Enums;

namespace HoleriteSign.Core.Interfaces;

/// <summary>
/// Append-only audit logging service.
/// Implements AUD-01–05, SEC-14, SEC-16 (hash chain).
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        string eventType,
        ActorType actorType,
        Guid? adminId = null,
        Guid? employeeId = null,
        Guid? documentId = null,
        string? eventData = null,
        string? actorIp = null,
        string? actorUserAgent = null);
}
