using HoleriteSign.Core.Enums;

namespace HoleriteSign.Core.Entities;

public class AuditLog
{
    public long Id { get; set; }

    // Context
    public Guid? AdminId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? DocumentId { get; set; }

    // Event
    public string EventType { get; set; } = string.Empty;
    public string? EventData { get; set; } // JSON

    // Actor
    public ActorType ActorType { get; set; }
    public string? ActorIp { get; set; }
    public string? ActorUserAgent { get; set; }

    // Hash chain (SEC-16) — scoped per document_id
    public string? PrevHash { get; set; } // SHA-256 of previous entry in chain
    public string EntryHash { get; set; } = string.Empty; // SHA-256 of this entry
    public int ChainVersion { get; set; } = 1;

    // Immutable timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
