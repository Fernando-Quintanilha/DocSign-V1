using System.Globalization;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Core.Entities;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoleriteSign.Api.Services;

public class PayPeriodService
{
    private readonly AppDbContext _db;

    public PayPeriodService(AppDbContext db)
    {
        _db = db;
    }

    private static readonly string[] MonthNames =
    {
        "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    };

    public async Task<List<PayPeriodDto>> ListAsync(Guid adminId)
    {
        return await _db.PayPeriods
            .Where(p => p.AdminId == adminId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Select(p => new PayPeriodDto(
                p.Id,
                p.Year,
                p.Month,
                p.Label ?? $"{MonthNames[p.Month]} {p.Year}",
                p.Documents.Count,
                p.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<PayPeriodDto> CreateAsync(CreatePayPeriodRequest request, Guid adminId)
    {
        if (request.Month < 1 || request.Month > 12)
            throw new InvalidOperationException("Mês deve estar entre 1 e 12.");

        if (request.Year < 2020 || request.Year > 2100)
            throw new InvalidOperationException("Ano inválido.");

        // Check for duplicate
        var exists = await _db.PayPeriods
            .AnyAsync(p => p.AdminId == adminId && p.Year == request.Year && p.Month == request.Month);

        if (exists)
            throw new InvalidOperationException($"Período {MonthNames[request.Month]} {request.Year} já existe.");

        var label = request.Label?.Trim() ?? $"{MonthNames[request.Month]} {request.Year}";

        var period = new PayPeriod
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Year = request.Year,
            Month = request.Month,
            Label = label,
            CreatedAt = DateTime.UtcNow,
        };

        _db.PayPeriods.Add(period);
        await _db.SaveChangesAsync();

        return new PayPeriodDto(period.Id, period.Year, period.Month, label, 0, period.CreatedAt);
    }

    /// <summary>
    /// Get or create a pay period for the given year/month.
    /// </summary>
    public async Task<PayPeriod> GetOrCreateAsync(int year, int month, Guid adminId)
    {
        var existing = await _db.PayPeriods
            .FirstOrDefaultAsync(p => p.AdminId == adminId && p.Year == year && p.Month == month);

        if (existing is not null)
            return existing;

        var label = $"{MonthNames[month]} {year}";
        var period = new PayPeriod
        {
            Id = Guid.NewGuid(),
            AdminId = adminId,
            Year = year,
            Month = month,
            Label = label,
            CreatedAt = DateTime.UtcNow,
        };

        _db.PayPeriods.Add(period);
        await _db.SaveChangesAsync();
        return period;
    }
}
