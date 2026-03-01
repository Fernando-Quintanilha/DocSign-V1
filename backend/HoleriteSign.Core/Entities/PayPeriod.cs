namespace HoleriteSign.Core.Entities;

public class PayPeriod
{
    public Guid Id { get; set; }
    public Guid AdminId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; } // 1–12
    public string? Label { get; set; } // e.g., "Fevereiro 2026"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Admin Admin { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
