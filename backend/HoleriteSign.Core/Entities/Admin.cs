using HoleriteSign.Core.Enums;

namespace HoleriteSign.Core.Entities;

public class Admin
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public AdminRole Role { get; set; } = AdminRole.Admin;
    public bool EmailVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Refresh token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // Email verification
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationExpiresAt { get; set; }

    // Password reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetExpiresAt { get; set; }

    // Navigation
    public Plan Plan { get; set; } = null!;
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<PayPeriod> PayPeriods { get; set; } = new List<PayPeriod>();
}
