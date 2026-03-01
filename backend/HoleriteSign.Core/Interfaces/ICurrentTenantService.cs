namespace HoleriteSign.Core.Interfaces;

/// <summary>
/// Provides the current tenant (admin) ID for query filtering.
/// Implements SEC-11: EF Core Global Query Filters by AdminId.
/// </summary>
public interface ICurrentTenantService
{
    Guid? AdminId { get; }
    bool IsSuperAdmin { get; }
}
