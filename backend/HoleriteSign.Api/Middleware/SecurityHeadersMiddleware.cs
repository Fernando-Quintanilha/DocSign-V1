namespace HoleriteSign.Api.Middleware;

/// <summary>
/// Adiciona cabeçalhos de segurança HTTP em toda resposta.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;

        // Impede que o browser faça MIME-sniffing
        h["X-Content-Type-Options"] = "nosniff";

        // Impede clickjacking (iframe embedding)
        h["X-Frame-Options"] = "DENY";

        // Ativa proteção XSS do browser (legado, mas não prejudica)
        h["X-XSS-Protection"] = "1; mode=block";

        // Impede vazamento de Referer
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Impede recursos do browser que não usamos
        h["Permissions-Policy"] = "camera=(self), microphone=(), geolocation=(), payment=()";

        // HSTS — ativado só quando em HTTPS para não quebrar dev local
        if (ctx.Request.IsHttps)
        {
            h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // CSP mínimo para API (não serve HTML, mas protege caso alguém acesse diretamente)
        h["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        // Remove header que revela tecnologia
        h.Remove("X-Powered-By");
        h.Remove("Server");

        await _next(ctx);
    }
}
