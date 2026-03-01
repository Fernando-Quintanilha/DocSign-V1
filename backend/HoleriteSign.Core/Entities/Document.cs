using HoleriteSign.Core.Enums;

namespace HoleriteSign.Core.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid PayPeriodId { get; set; }
    public Guid AdminId { get; set; }

    // Original file
    public string OriginalFilename { get; set; } = string.Empty;
    public string OriginalFileKey { get; set; } = string.Empty; // S3/R2 key
    public string OriginalFileHash { get; set; } = string.Empty; // SHA-256
    public long FileSizeBytes { get; set; }

    // Signed file (populated after signature)
    public string? SignedFileKey { get; set; }
    public string? SignedFileHash { get; set; }

    // Status tracking
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;

    // Signing token (SEC-15: store hash only)
    public string? SigningTokenHash { get; set; } // SHA-256 of the raw token
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? TokenUsedAt { get; set; }

    // Tracking timestamps (DATA-02)
    public DateTime? ViewedAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Employee Employee { get; set; } = null!;
    public PayPeriod PayPeriod { get; set; } = null!;
    public Admin Admin { get; set; } = null!;
    public Signature? Signature { get; set; }
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public SigningVerification? SigningVerification { get; set; }
}
