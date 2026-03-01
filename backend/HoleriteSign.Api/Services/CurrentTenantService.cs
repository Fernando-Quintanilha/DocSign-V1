using System.Security.Claims;
using HoleriteSign.Core.Interfaces;

namespace HoleriteSign.Api.Services;

/// <summary>
/// Reads the current authenticated admin from the JWT claims.
/// Used by EF Core Global Query Filters for tenant isolation (SEC-11).
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? AdminId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User
                .FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (string.IsNullOrEmpty(sub))
            {
                // Fallback: check ClaimTypes.NameIdentifier (ASP.NET maps "sub" here)
                sub = _httpContextAccessor.HttpContext?.User
                    .FindFirstValue(ClaimTypes.NameIdentifier);
            }

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public bool IsSuperAdmin
    {
        get
        {
            var role = _httpContextAccessor.HttpContext?.User
                .FindFirstValue("role");
            return role == "SuperAdmin";
        }
    }
}
