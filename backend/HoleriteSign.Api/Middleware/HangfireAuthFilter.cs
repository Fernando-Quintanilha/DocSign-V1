using Hangfire.Dashboard;

namespace HoleriteSign.Api.Middleware;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// In development, allow anyone. In production, restrict access.
/// </summary>
public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var env = httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

        // In development, always allow
        if (env.IsDevelopment()) return true;

        // In production, only allow if accessed from localhost (e.g., SSH tunnel)
        return httpContext.Connection.RemoteIpAddress?.ToString() is "127.0.0.1" or "::1";
    }
}
