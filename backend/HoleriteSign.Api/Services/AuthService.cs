using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HoleriteSign.Api.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly EmailService _email;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, EmailService email, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _email = email;
        _logger = logger;
    }

    // ── Register ─────────────────────────────────────────

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var exists = await _db.Admins.AnyAsync(a => a.Email == request.Email.ToLower().Trim());
        if (exists)
            throw new InvalidOperationException("E-mail já cadastrado.");

        if (request.Password.Length < 8)
            throw new InvalidOperationException("Senha deve ter no mínimo 8 caracteres.");

        var freePlan = await _db.Plans.FirstOrDefaultAsync(p => p.Name == "free")
            ?? throw new InvalidOperationException("Plano gratuito não encontrado.");

        var admin = new Admin
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CompanyName = request.CompanyName.Trim(),
            PlanId = freePlan.Id,
            Role = AdminRole.Admin,
            EmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Admins.Add(admin);
        await _db.SaveChangesAsync();

        await _db.Entry(admin).Reference(a => a.Plan).LoadAsync();

        // Send verification email
        try
        {
            await SendVerificationEmailInternalAsync(admin);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send verification email to {Email}", admin.Email);
        }

        var (jwt, refreshToken) = await GenerateTokensAsync(admin);
        return new AuthResponse(jwt, refreshToken, MapToDto(admin));
    }

    // ── Login ────────────────────────────────────────────

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var admin = await _db.Admins
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Email == request.Email.ToLower().Trim());

        if (admin is null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            throw new UnauthorizedAccessException("E-mail ou senha inválidos.");

        if (!admin.IsActive)
            throw new UnauthorizedAccessException("Conta desativada.");

        var (jwt, refreshToken) = await GenerateTokensAsync(admin);
        return new AuthResponse(jwt, refreshToken, MapToDto(admin));
    }

    // ── Refresh Token ────────────────────────────────────

    public async Task<RefreshTokenResponse> RefreshAsync(RefreshTokenRequest request)
    {
        var tokenHash = HashToken(request.RefreshToken);

        var admin = await _db.Admins
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.RefreshToken == tokenHash);

        if (admin is null || admin.RefreshTokenExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token inválido ou expirado.");

        if (!admin.IsActive)
            throw new UnauthorizedAccessException("Conta desativada.");

        var (jwt, refreshToken) = await GenerateTokensAsync(admin);
        return new RefreshTokenResponse(jwt, refreshToken);
    }

    // ── Logout (invalidate refresh token) ────────────────

    public async Task LogoutAsync(Guid adminId)
    {
        var admin = await _db.Admins.FindAsync(adminId);
        if (admin != null)
        {
            admin.RefreshToken = null;
            admin.RefreshTokenExpiresAt = null;
            await _db.SaveChangesAsync();
        }
    }

    // ── Email Verification ───────────────────────────────

    public async Task SendVerificationEmailAsync(Guid adminId)
    {
        var admin = await _db.Admins.FindAsync(adminId)
            ?? throw new InvalidOperationException("Admin não encontrado.");

        if (admin.EmailVerified)
            throw new InvalidOperationException("E-mail já verificado.");

        await SendVerificationEmailInternalAsync(admin);
    }

    private async Task SendVerificationEmailInternalAsync(Admin admin)
    {
        var token = GenerateSecureToken();
        admin.EmailVerificationToken = HashToken(token);
        admin.EmailVerificationExpiresAt = DateTime.UtcNow.AddHours(24);
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var frontendUrl = _config["App:FrontendUrl"] ?? "http://localhost:5173";
        await _email.SendVerificationEmailAsync(admin.Email, admin.Name, token, frontendUrl);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request)
    {
        var tokenHash = HashToken(request.Token);

        var admin = await _db.Admins
            .FirstOrDefaultAsync(a => a.EmailVerificationToken == tokenHash);

        if (admin is null || admin.EmailVerificationExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Token de verificação inválido ou expirado.");

        admin.EmailVerified = true;
        admin.EmailVerificationToken = null;
        admin.EmailVerificationExpiresAt = null;
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── Forgot Password ──────────────────────────────────

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var admin = await _db.Admins
            .FirstOrDefaultAsync(a => a.Email == request.Email.ToLower().Trim());

        // Always return success to prevent email enumeration
        if (admin is null || !admin.IsActive) return;

        var token = GenerateSecureToken();
        admin.PasswordResetToken = HashToken(token);
        admin.PasswordResetExpiresAt = DateTime.UtcNow.AddHours(1);
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var frontendUrl = _config["App:FrontendUrl"] ?? "http://localhost:5173";
        await _email.SendPasswordResetEmailAsync(admin.Email, admin.Name, token, frontendUrl);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword.Length < 8)
            throw new InvalidOperationException("Nova senha deve ter no mínimo 8 caracteres.");

        var tokenHash = HashToken(request.Token);

        var admin = await _db.Admins
            .FirstOrDefaultAsync(a => a.PasswordResetToken == tokenHash);

        if (admin is null || admin.PasswordResetExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Token de redefinição inválido ou expirado.");

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        admin.PasswordResetToken = null;
        admin.PasswordResetExpiresAt = null;
        // Invalidate refresh token on password reset for security
        admin.RefreshToken = null;
        admin.RefreshTokenExpiresAt = null;
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── Profile ──────────────────────────────────────────

    public async Task ChangePasswordAsync(Guid adminId, ChangePasswordRequest request)
    {
        var admin = await _db.Admins.FindAsync(adminId)
            ?? throw new InvalidOperationException("Admin não encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, admin.PasswordHash))
            throw new UnauthorizedAccessException("Senha atual incorreta.");

        if (request.NewPassword.Length < 8)
            throw new InvalidOperationException("Nova senha deve ter no mínimo 8 caracteres.");

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<AdminDto> UpdateProfileAsync(Guid adminId, UpdateProfileRequest request)
    {
        var admin = await _db.Admins
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == adminId)
            ?? throw new InvalidOperationException("Admin não encontrado.");

        admin.Name = request.Name.Trim();
        admin.CompanyName = request.CompanyName.Trim();
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapToDto(admin);
    }

    public async Task<AdminDto> GetProfileAsync(Guid adminId)
    {
        var admin = await _db.Admins
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == adminId)
            ?? throw new InvalidOperationException("Admin não encontrado.");

        return MapToDto(admin);
    }

    // ── Token Generation ─────────────────────────────────

    private async Task<(string jwt, string refreshToken)> GenerateTokensAsync(Admin admin)
    {
        var jwt = GenerateJwt(admin);
        var refreshToken = GenerateSecureToken();

        // Store hashed refresh token in DB
        admin.RefreshToken = HashToken(refreshToken);
        admin.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        admin.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (jwt, refreshToken);
    }

    private string GenerateJwt(Admin admin)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, admin.Email),
            new Claim("name", admin.Name),
            new Claim("company", admin.CompanyName),
            new Claim("role", admin.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var expMinutes = int.Parse(_config["Jwt:ExpirationMinutes"] ?? "60");
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ── Helpers ──────────────────────────────────────────

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    private static AdminDto MapToDto(Admin admin) => new(
        admin.Id,
        admin.Name,
        admin.Email,
        admin.CompanyName,
        admin.Role.ToString(),
        admin.Plan.DisplayName,
        admin.EmailVerified
    );
}
