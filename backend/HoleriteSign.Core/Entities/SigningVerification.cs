using HoleriteSign.Core.Enums;

namespace HoleriteSign.Core.Entities;

public class SigningVerification
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid EmployeeId { get; set; }

    // Verification method and state
    public VerificationMethod Method { get; set; }
    public bool Verified { get; set; }
    public DateTime? VerifiedAt { get; set; }

    // OTP-specific fields (null for CPF/DOB)
    public string? OtpHash { get; set; } // SHA-256 of the OTP code
    public DateTime? OtpExpiresAt { get; set; } // 5 min validity
    public DateTime? LastSentAt { get; set; } // 60s cooldown
    public int AttemptCount { get; set; }
    public DateTime? AttemptWindowStart { get; set; } // 3 attempts per 10 min

    // Session
    public DateTime ExpiresAt { get; set; } // Same as token expiry

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Document Document { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
