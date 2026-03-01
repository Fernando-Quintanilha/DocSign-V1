using System.Net;
using System.Net.Mail;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Core.Interfaces;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

public class NotificationService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly SigningService _signingService;
    private readonly IConfiguration _config;
    private readonly WhatsAppService _whatsApp;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AppDbContext db,
        IAuditService audit,
        SigningService signingService,
        IConfiguration config,
        WhatsAppService whatsApp,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _audit = audit;
        _signingService = signingService;
        _config = config;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    /// <summary>
    /// Send notification for a single document.
    /// Generates token if not already generated.
    /// </summary>
    public async Task<NotificationDto> SendAsync(Guid documentId, string channel, Guid adminId)
    {
        var document = await _db.Documents
            .Include(d => d.Employee)
            .Include(d => d.PayPeriod)
            .FirstOrDefaultAsync(d => d.Id == documentId)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        if (document.Status == DocumentStatus.Signed)
            throw new InvalidOperationException("Documento já foi assinado.");

        // Generate signing token if not yet generated
        string? signingUrl = null;
        if (string.IsNullOrEmpty(document.SigningTokenHash))
        {
            var tokenResponse = await _signingService.GenerateTokenAsync(documentId, adminId);
            signingUrl = tokenResponse.SigningUrl;
        }
        else
        {
            // Token already exists — we can't recover the raw token from the hash
            // Generate a new one
            var tokenResponse = await _signingService.GenerateTokenAsync(documentId, adminId);
            signingUrl = tokenResponse.SigningUrl;
        }

        var parsedChannel = Enum.Parse<NotificationChannel>(channel, true);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            EmployeeId = document.EmployeeId,
            Channel = parsedChannel,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        try
        {
            switch (parsedChannel)
            {
                case NotificationChannel.Email:
                    await SendEmailAsync(document.Employee, document, signingUrl!);
                    break;
                case NotificationChannel.WhatsApp:
                    await SendWhatsAppAsync(document.Employee, document, signingUrl!);
                    break;
            }

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;

            document.LastNotifiedAt = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = ex.Message;
        }

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(
            $"notification_{notification.Status.ToString().ToLower()}",
            ActorType.Admin,
            adminId: adminId,
            employeeId: document.EmployeeId,
            documentId: documentId,
            eventData: $"{{\"channel\":\"{channel}\",\"status\":\"{notification.Status}\"}}");

        return new NotificationDto(
            notification.Id,
            notification.DocumentId,
            document.Employee.Name,
            notification.Channel.ToString(),
            notification.Status.ToString(),
            notification.SentAt,
            notification.ErrorMessage,
            notification.CreatedAt
        );
    }

    /// <summary>
    /// Send notifications for all unsigned documents in a pay period.
    /// </summary>
    public async Task<List<NotificationDto>> SendBulkAsync(Guid payPeriodId, string channel, Guid adminId)
    {
        var documents = await _db.Documents
            .Include(d => d.Employee)
            .Include(d => d.PayPeriod)
            .Where(d => d.PayPeriodId == payPeriodId && d.Status != DocumentStatus.Signed)
            .ToListAsync();

        var results = new List<NotificationDto>();
        foreach (var doc in documents)
        {
            try
            {
                var result = await SendAsync(doc.Id, channel, adminId);
                results.Add(result);
            }
            catch (Exception)
            {
                // Continue sending to others
            }
        }

        return results;
    }

    /// <summary>
    /// List notifications for the current tenant.
    /// </summary>
    public async Task<List<NotificationDto>> ListAsync(Guid? documentId = null)
    {
        var query = _db.Notifications
            .Include(n => n.Employee)
            .Include(n => n.Document)
            .AsQueryable();

        if (documentId.HasValue)
            query = query.Where(n => n.DocumentId == documentId.Value);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new NotificationDto(
                n.Id,
                n.DocumentId,
                n.Employee.Name,
                n.Channel.ToString(),
                n.Status.ToString(),
                n.SentAt,
                n.ErrorMessage,
                n.CreatedAt
            ))
            .ToListAsync();
    }

    private async Task SendEmailAsync(Employee employee, Document document, string signingUrl)
    {
        if (string.IsNullOrEmpty(employee.Email))
            throw new InvalidOperationException("Funcionário não possui e-mail cadastrado.");

        var smtpHost = _config["Smtp:Host"];
        var smtpPort = int.TryParse(_config["Smtp:Port"], out var port) ? port : 587;
        var smtpUser = _config["Smtp:User"];
        var smtpPass = _config["Smtp:Password"];
        var fromEmail = _config["Smtp:From"] ?? "noreply@holeritesign.com";

        // MVP: If SMTP is not configured, log and simulate success
        if (string.IsNullOrEmpty(smtpHost))
        {
            Console.WriteLine($"[EMAIL SIMULADO] Para: {employee.Email} | Link: {signingUrl}");
            return;
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPass),
        };

        var subject = $"Holerite disponível - {document.PayPeriod.Label ?? "Período"}";
        var body = $"""
            Olá {employee.Name},

            Seu holerite está disponível para visualização e assinatura.

            Clique no link abaixo para acessar:
            {signingUrl}

            Este link é válido por 72 horas.

            Atenciosamente,
            Equipe HoleriteSign
            """;

        var message = new MailMessage(fromEmail, employee.Email, subject, body);
        await client.SendMailAsync(message);
    }

    private async Task SendWhatsAppAsync(Employee employee, Document document, string signingUrl)
    {
        if (string.IsNullOrEmpty(employee.WhatsApp))
            throw new InvalidOperationException("Funcionário não possui WhatsApp cadastrado.");

        var msg = $"📄 *HoleriteSign*\n\n" +
                  $"Olá *{employee.Name}*!\n\n" +
                  $"Seu holerite de *{document.PayPeriod.Label}* está disponível para assinatura.\n\n" +
                  $"🔗 Acesse o link abaixo para visualizar e assinar:\n{signingUrl}\n\n" +
                  $"⏰ Este link é válido por 72 horas.";

        var result = await _whatsApp.SendTextMessageAsync(employee.WhatsApp, msg);

        if (result?.Key?.Id != null)
        {
            _logger.LogInformation(
                "WhatsApp enviado para {Phone} (msgId: {MsgId})",
                employee.WhatsApp, result.Key.Id);
        }
    }
}
