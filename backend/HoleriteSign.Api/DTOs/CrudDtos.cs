namespace HoleriteSign.Api.DTOs;

// ── Employee ──────────────────────────────────────────────

public record CreateEmployeeRequest(
    string Name,
    string? Email,
    string? WhatsApp,
    string? Cpf,
    string? BirthDate // yyyy-MM-dd
);

public record UpdateEmployeeRequest(
    string Name,
    string? Email,
    string? WhatsApp,
    string? Cpf,
    string? BirthDate
);

public record EmployeeDto(
    Guid Id,
    string Name,
    string? Email,
    string? WhatsApp,
    string? CpfLast4,
    bool HasBirthDate,
    bool IsActive,
    DateTime CreatedAt
);

// ── PayPeriod ─────────────────────────────────────────────

public record CreatePayPeriodRequest(
    int Year,
    int Month,
    string? Label
);

public record PayPeriodDto(
    Guid Id,
    int Year,
    int Month,
    string Label,
    int DocumentCount,
    DateTime CreatedAt
);

// ── Document ──────────────────────────────────────────────

public record UploadDocumentRequest(
    Guid EmployeeId,
    Guid PayPeriodId
    // File comes as IFormFile via multipart
);

public record DocumentDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    Guid PayPeriodId,
    string PayPeriodLabel,
    string OriginalFilename,
    long FileSizeBytes,
    string Status,
    DateTime? ViewedAt,
    DateTime? LastNotifiedAt,
    DateTime CreatedAt,
    DateTime? SignedAt
);

// ── Dashboard ─────────────────────────────────────────────

public record DashboardStatsDto(
    int TotalEmployees,
    int ActiveEmployees,
    int TotalDocuments,
    int PendingDocuments,
    int SignedDocuments,
    int ExpiredDocuments,
    string PlanName,
    int PlanMaxEmployees,
    int PlanMaxDocuments
);

// ── Enhanced Dashboard (Phase 7) ──────────────────────────

public record EnhancedDashboardDto(
    // Basic stats
    int TotalEmployees,
    int ActiveEmployees,
    int TotalDocuments,
    int PendingDocuments,
    int SignedDocuments,
    int ExpiredDocuments,
    // Plan info
    string PlanName,
    int PlanMaxEmployees,
    int PlanMaxDocuments,
    int DocumentsUsedThisMonth,
    // Per-period breakdown
    List<PeriodSummaryDto> Periods,
    // Quem falta assinar (current/latest period)
    List<PendingEmployeeDto> PendingEmployees,
    // Recent activity
    List<RecentActivityDto> RecentActivity
);

public record PeriodSummaryDto(
    Guid Id,
    int Year,
    int Month,
    string Label,
    int TotalDocuments,
    int SignedDocuments,
    int PendingDocuments,
    int ExpiredDocuments
);

public record PendingEmployeeDto(
    Guid EmployeeId,
    string EmployeeName,
    string? Email,
    string? WhatsApp,
    Guid? DocumentId,
    string DocumentStatus, // "Uploaded", "Sent", "Expired" or "NoDocument"
    DateTime? LastNotifiedAt
);

public record RecentActivityDto(
    long Id,
    string EventType,
    string ActorType,
    string? EmployeeName,
    string? DocumentFilename,
    DateTime CreatedAt
);
