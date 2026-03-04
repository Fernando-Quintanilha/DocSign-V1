using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Core.Entities;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

public class EmployeeService
{
    private readonly AppDbContext _db;
    private readonly EncryptionService _encryption;

    public EmployeeService(AppDbContext db, EncryptionService encryption)
    {
        _db = db;
        _encryption = encryption;
    }

    public async Task<List<EmployeeDto>> ListAsync(Guid adminId, string? search = null)
    {
        var query = _db.Employees
            .Where(e => e.AdminId == adminId && !e.DeletedAt.HasValue);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.Name.ToLower().Contains(term) ||
                (e.Email != null && e.Email.ToLower().Contains(term)) ||
                (e.CpfLast4 != null && e.CpfLast4.Contains(term)));
        }

        return await query
            .OrderBy(e => e.Name)
            .Select(e => new EmployeeDto(
                e.Id,
                e.Name,
                e.Email,
                e.WhatsApp,
                e.CpfLast4,
                e.BirthDateEncrypted != null,
                e.IsActive,
                e.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, Guid adminId)
    {
        return await _db.Employees
            .Where(e => e.Id == id && e.AdminId == adminId && !e.DeletedAt.HasValue)
            .Select(e => new EmployeeDto(
                e.Id,
                e.Name,
                e.Email,
                e.WhatsApp,
                e.CpfLast4,
                e.BirthDateEncrypted != null,
                e.IsActive,
                e.CreatedAt
            ))
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get employee detail with decrypted PII (CPF, BirthDate) for the edit form.
    /// </summary>
    public async Task<EmployeeDetailDto?> GetDetailByIdAsync(Guid id, Guid adminId)
    {
        var emp = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id && e.AdminId == adminId && !e.DeletedAt.HasValue);

        if (emp == null) return null;

        string? cpf = null;
        string? birthDate = null;

        if (emp.CpfEncrypted != null)
        {
            try { cpf = _encryption.Decrypt(emp.CpfEncrypted); }
            catch { /* ignore decryption errors */ }
        }

        if (emp.BirthDateEncrypted != null)
        {
            try
            {
                var raw = _encryption.Decrypt(emp.BirthDateEncrypted);
                // Normalize to yyyy-MM-dd for HTML <input type="date"> compatibility
                birthDate = DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                         || DateOnly.TryParse(raw, new CultureInfo("pt-BR"), DateTimeStyles.None, out d)
                         || DateOnly.TryParse(raw, out d)
                    ? d.ToString("yyyy-MM-dd")
                    : raw;
            }
            catch { /* ignore decryption errors */ }
        }

        return new EmployeeDetailDto(
            emp.Id,
            emp.Name,
            emp.Email,
            emp.WhatsApp,
            emp.CpfLast4,
            cpf,
            birthDate,
            emp.IsActive,
            emp.CreatedAt
        );
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request, Guid adminId)
    {
        // ── Plan limit enforcement ──
        var admin = await _db.Admins.Include(a => a.Plan).FirstAsync(a => a.Id == adminId);
        if (admin.Plan.MaxEmployees > 0)
        {
            var currentCount = await _db.Employees
                .Where(e => e.AdminId == adminId && !e.DeletedAt.HasValue)
                .CountAsync();
            if (currentCount >= admin.Plan.MaxEmployees)
                throw new InvalidOperationException(
                    $"Limite do plano atingido ({admin.Plan.MaxEmployees} funcionários). Faça upgrade para continuar.");
        }

        // Validate at least one contact method
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.WhatsApp))
            throw new InvalidOperationException("Informe e-mail ou WhatsApp.");

        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Name = request.Name.Trim(),
            Email = request.Email?.Trim().ToLower(),
            WhatsApp = request.WhatsApp?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // CPF: store AES-256 encrypted + last 4
        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpfClean = request.Cpf.Replace(".", "").Replace("-", "").Trim();
            if (cpfClean.Length != 11)
                throw new InvalidOperationException("CPF deve ter 11 dígitos.");

            employee.CpfLast4 = cpfClean[^4..];
            employee.CpfEncrypted = _encryption.Encrypt(cpfClean);
        }

        // Birth date: normalize to yyyy-MM-dd and store AES-256 encrypted
        if (!string.IsNullOrWhiteSpace(request.BirthDate))
        {
            if (!DateOnly.TryParse(request.BirthDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
             && !DateOnly.TryParse(request.BirthDate, new CultureInfo("pt-BR"), DateTimeStyles.None, out parsedDate)
             && !DateOnly.TryParse(request.BirthDate, out parsedDate))
                throw new InvalidOperationException("Data de nascimento inválida. Use formato yyyy-MM-dd.");

            employee.BirthDateEncrypted = _encryption.Encrypt(parsedDate.ToString("yyyy-MM-dd"));
        }

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return new EmployeeDto(
            employee.Id,
            employee.Name,
            employee.Email,
            employee.WhatsApp,
            employee.CpfLast4,
            employee.BirthDateEncrypted != null,
            employee.IsActive,
            employee.CreatedAt
        );
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, Guid adminId)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id && e.AdminId == adminId && !e.DeletedAt.HasValue)
            ?? throw new InvalidOperationException("Funcionário não encontrado.");

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.WhatsApp))
            throw new InvalidOperationException("Informe e-mail ou WhatsApp.");

        employee.Name = request.Name.Trim();
        employee.Email = request.Email?.Trim().ToLower();
        employee.WhatsApp = request.WhatsApp?.Trim();
        employee.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Cpf))
        {
            var cpfClean = request.Cpf.Replace(".", "").Replace("-", "").Trim();
            if (cpfClean.Length != 11)
                throw new InvalidOperationException("CPF deve ter 11 dígitos.");

            employee.CpfLast4 = cpfClean[^4..];
            employee.CpfEncrypted = _encryption.Encrypt(cpfClean);
        }

        if (!string.IsNullOrWhiteSpace(request.BirthDate))
        {
            if (!DateOnly.TryParse(request.BirthDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
             && !DateOnly.TryParse(request.BirthDate, new CultureInfo("pt-BR"), DateTimeStyles.None, out parsedDate)
             && !DateOnly.TryParse(request.BirthDate, out parsedDate))
                throw new InvalidOperationException("Data de nascimento inválida. Use formato yyyy-MM-dd.");

            employee.BirthDateEncrypted = _encryption.Encrypt(parsedDate.ToString("yyyy-MM-dd"));
        }

        await _db.SaveChangesAsync();

        return new EmployeeDto(
            employee.Id,
            employee.Name,
            employee.Email,
            employee.WhatsApp,
            employee.CpfLast4,
            employee.BirthDateEncrypted != null,
            employee.IsActive,
            employee.CreatedAt
        );
    }

    public async Task DeleteAsync(Guid id, Guid adminId)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == id && e.AdminId == adminId && !e.DeletedAt.HasValue)
            ?? throw new InvalidOperationException("Funcionário não encontrado.");

        employee.IsActive = false;
        employee.DeletedAt = DateTime.UtcNow;
        employee.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// EMP-02: Import employees from a CSV file.
    /// Expected columns: Nome, Email, WhatsApp, CPF, DataNascimento
    /// </summary>
    public async Task<ImportResult> ImportCsvAsync(Stream csvStream, Guid adminId)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8);
        var headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidOperationException("Arquivo CSV vazio.");

        var created = 0;
        var skipped = 0;
        var errors = new List<string>();
        var lineNum = 1;

        while (!reader.EndOfStream)
        {
            lineNum++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = ParseCsvLine(line);
            if (cols.Length < 2)
            {
                errors.Add($"Linha {lineNum}: colunas insuficientes");
                skipped++;
                continue;
            }

            var name = cols[0].Trim();
            var email = cols.Length > 1 ? cols[1].Trim().ToLower() : null;
            var whatsapp = cols.Length > 2 ? cols[2].Trim() : null;
            var cpf = cols.Length > 3 ? cols[3].Trim() : null;
            var birthDate = cols.Length > 4 ? cols[4].Trim() : null;

            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Linha {lineNum}: nome vazio");
                skipped++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(whatsapp))
            {
                errors.Add($"Linha {lineNum}: informe email ou whatsapp");
                skipped++;
                continue;
            }

            // Check duplicate by email
            if (!string.IsNullOrWhiteSpace(email))
            {
                var exists = await _db.Employees.AnyAsync(e =>
                    e.AdminId == adminId && e.Email == email && !e.DeletedAt.HasValue);
                if (exists)
                {
                    errors.Add($"Linha {lineNum}: email '{email}' já cadastrado");
                    skipped++;
                    continue;
                }
            }

            try
            {
                var request = new CreateEmployeeRequest(name, email, whatsapp, cpf, birthDate);
                await CreateAsync(request, adminId);
                created++;
            }
            catch (Exception ex)
            {
                errors.Add($"Linha {lineNum}: {ex.Message}");
                skipped++;
            }
        }

        return new ImportResult(created, skipped, errors);
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();

        foreach (var ch in line)
        {
            if (ch == '"') { inQuotes = !inQuotes; continue; }
            if (ch == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }
            if (ch == ';' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
                continue;
            }
            current.Append(ch);
        }
        result.Add(current.ToString());
        return result.ToArray();
    }
}

public record ImportResult(int Created, int Skipped, List<string> Errors);
