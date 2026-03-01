using HoleriteSign.Core.Enums;

namespace HoleriteSign.Core.Entities;

public class Signature
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid EmployeeId { get; set; }

    // Selfie photo
    public string PhotoFileKey { get; set; } = string.Empty; // S3/R2 key
    public string PhotoHash { get; set; } = string.Empty; // SHA-256
    public string PhotoMimeType { get; set; } = string.Empty;

    // Signer identification metadata
    public string SignerIp { get; set; } = string.Empty;
    public string SignerUserAgent { get; set; } = string.Empty;
    public string? SignerGeolocation { get; set; } // JSON: { lat, lng, accuracy }
    public string? SignerDeviceInfo { get; set; } // JSON

    // Timestamp
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    // Legal
    public bool ConsentGiven { get; set; } = true;
    public string ConsentText { get; set; } = string.Empty;

    // Verification (SIG-11)
    public VerificationMethod? VerificationMethod { get; set; }
    public string? VerificationHash { get; set; } // SHA-256 of submitted value

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Document Document { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
