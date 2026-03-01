namespace HoleriteSign.Api.DTOs;

// ── Token Generation (Admin) ──────────────────────────────

public record GenerateTokenResponse(
    string SigningUrl,
    DateTime ExpiresAt
);

// ── Public Signing Flow ───────────────────────────────────

public record ValidateTokenResponse(
    bool Valid,
    string EmployeeName,
    string CompanyName,
    string PayPeriodLabel,
    bool RequiresCpf,
    bool RequiresBirthDate
);

public record VerifyIdentityRequest(
    string? Cpf,
    string? BirthDate // yyyy-MM-dd
);

public record VerifyIdentityResponse(
    bool Verified,
    string? Message
);

public record SigningDocumentDto(
    Guid DocumentId,
    string EmployeeName,
    string CompanyName,
    string PayPeriodLabel,
    string OriginalFilename,
    long FileSizeBytes,
    string DownloadUrl // Presigned or direct URL
);

public record SignDocumentRequest(
    string PhotoBase64, // Selfie in base64
    string PhotoMimeType, // image/jpeg or image/png
    bool ConsentGiven,
    string? Geolocation // JSON: { lat, lng, accuracy }
);

public record SignDocumentResponse(
    bool Success,
    string Message,
    DateTime SignedAt
);

// ── Notifications ─────────────────────────────────────────

public record SendNotificationRequest(
    Guid DocumentId,
    string Channel // "Email" or "WhatsApp"
);

public record SendBulkNotificationRequest(
    Guid PayPeriodId,
    string Channel
);

public record NotificationDto(
    Guid Id,
    Guid DocumentId,
    string EmployeeName,
    string Channel,
    string Status,
    DateTime? SentAt,
    string? ErrorMessage,
    DateTime CreatedAt
);

// ── Audit ─────────────────────────────────────────────────

public record AuditLogDto(
    long Id,
    string EventType,
    string ActorType,
    string? ActorIp,
    Guid? AdminId,
    Guid? EmployeeId,
    Guid? DocumentId,
    string? EventData,
    DateTime CreatedAt
);
