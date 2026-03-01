using System.Net;
using System.Net.Mail;

namespace HoleriteSign.Api.Services;

/// <summary>
/// Centralized email sending service.
/// If SMTP is not configured, emails are logged to console.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>Whether SMTP is configured (i.e. real emails will be sent).</summary>
    public bool IsConfigured => !string.IsNullOrEmpty(_config["Smtp:Host"]);

    /// <summary>
    /// Send an email. Falls back to console log when SMTP is not configured.
    /// </summary>
    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var smtpHost = _config["Smtp:Host"];
        var smtpPort = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var smtpUser = _config["Smtp:User"];
        var smtpPass = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:From"] ?? "noreply@holeritesign.com";
        var fromName = _config["Smtp:FromName"] ?? "HoleriteSign";

        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogWarning("[EMAIL SIMULADO] Para: {To} | Assunto: {Subject}", to, subject);
            _logger.LogInformation("[EMAIL BODY]\n{Body}", htmlBody);
            return;
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPass),
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        await client.SendMailAsync(message);
        _logger.LogInformation("Email enviado para {To}: {Subject}", to, subject);
    }

    // ── Email templates ──────────────────────────────────

    public async Task SendVerificationEmailAsync(string to, string name, string token, string frontendUrl)
    {
        var verifyUrl = $"{frontendUrl}/verify-email?token={token}";
        var subject = "Verifique seu e-mail — HoleriteSign";
        var body = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
                <h2 style="color:#2563EB;">HoleriteSign</h2>
                <p>Olá <strong>{name}</strong>,</p>
                <p>Obrigado por se cadastrar! Clique no botão abaixo para verificar seu e-mail:</p>
                <p style="text-align:center;margin:30px 0;">
                    <a href="{verifyUrl}" 
                       style="background:#2563EB;color:#fff;padding:12px 30px;border-radius:6px;text-decoration:none;font-weight:bold;">
                        Verificar E-mail
                    </a>
                </p>
                <p style="color:#666;font-size:12px;">Ou copie e cole este link no navegador:<br/>{verifyUrl}</p>
                <p style="color:#666;font-size:12px;">Este link é válido por 24 horas.</p>
            </div>
            """;
        await SendAsync(to, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string to, string name, string token, string frontendUrl)
    {
        var resetUrl = $"{frontendUrl}/reset-password?token={token}";
        var subject = "Redefinir senha — HoleriteSign";
        var body = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
                <h2 style="color:#2563EB;">HoleriteSign</h2>
                <p>Olá <strong>{name}</strong>,</p>
                <p>Recebemos uma solicitação para redefinir sua senha. Clique no botão abaixo:</p>
                <p style="text-align:center;margin:30px 0;">
                    <a href="{resetUrl}" 
                       style="background:#DC2626;color:#fff;padding:12px 30px;border-radius:6px;text-decoration:none;font-weight:bold;">
                        Redefinir Senha
                    </a>
                </p>
                <p style="color:#666;font-size:12px;">Ou copie e cole este link no navegador:<br/>{resetUrl}</p>
                <p style="color:#666;font-size:12px;">Este link é válido por 1 hora. Se você não solicitou, ignore este e-mail.</p>
            </div>
            """;
        await SendAsync(to, subject, body);
    }
}
