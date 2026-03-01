using HoleriteSign.Core.Enums;

namespace HoleriteSign.Core.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid EmployeeId { get; set; }

    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public string? ExternalId { get; set; } // ID from SendGrid/WhatsApp API
    public string? ErrorMessage { get; set; }

    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Document Document { get; set; } = null!;
    public Employee Employee { get; set; } = null!;
}
