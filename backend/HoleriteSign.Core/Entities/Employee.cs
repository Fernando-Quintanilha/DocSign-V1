namespace HoleriteSign.Core.Entities;

public class Employee
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? WhatsApp { get; set; } // E.164 format: +5511999999999

    // PII — encrypted at application level (AES-256 via EF Core Value Converters)
    public byte[]? CpfEncrypted { get; set; }
    public string? CpfLast4 { get; set; } // Last 4 digits for masked display
    public byte[]? BirthDateEncrypted { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; } // Soft delete

    // Navigation
    public Admin Admin { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Signature> Signatures { get; set; } = new List<Signature>();
}
