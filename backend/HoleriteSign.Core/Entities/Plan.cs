namespace HoleriteSign.Core.Entities;

public class Plan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // 'free', 'basic', 'pro', 'enterprise'
    public string DisplayName { get; set; } = string.Empty; // 'Plano Gratuito', etc.
    public int MaxDocuments { get; set; } = 10; // Monthly limit (-1 = unlimited)
    public int MaxEmployees { get; set; } = 5; // -1 = unlimited
    public decimal PriceMonthly { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Admin> Admins { get; set; } = new List<Admin>();
}
