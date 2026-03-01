using System.Text;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using HoleriteSign.Api.Middleware;
using HoleriteSign.Api.Services;
using HoleriteSign.Api.Validators;
using HoleriteSign.Core.Interfaces;
using HoleriteSign.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using Serilog;

// QuestPDF Community License
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/holeritesign-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

// =========================================================
// Services
// =========================================================

// Database (PostgreSQL via EF Core)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Rate Limiting (AspNetCoreRateLimit) ───────────────────
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(
    builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero,
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgresql")
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis");

// Tenant isolation (reads JWT claims)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();

// Storage (MinIO)
builder.Services.AddSingleton<IStorageService, HoleriteSign.Infrastructure.Services.MinioStorageService>();

// Encryption (AES-256 for PII)
builder.Services.AddSingleton<EncryptionService>();

// Email service
builder.Services.AddSingleton<EmailService>();

// Application services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmployeeService>();
builder.Services.AddScoped<PayPeriodService>();
builder.Services.AddScoped<DocumentService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<IAuditService, AuditLogService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<SigningService>();
builder.Services.AddScoped<SignedPdfService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ExportService>();

// Background jobs
builder.Services.AddHostedService<TokenExpirationJob>();

// ── FluentValidation ──────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// CORS — allow frontend dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["App:FrontendUrl"] ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "HoleriteSign API",
        Version = "v1",
        Description = "API para assinatura digital de holerites com selfie e trilha de auditoria.",
        Contact = new OpenApiContact { Name = "HoleriteSign" },
    });

    // JWT Bearer auth in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Informe: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// =========================================================
// Middleware Pipeline
// =========================================================

// Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "HoleriteSign API v1");
        c.RoutePrefix = "swagger";
    });
}

// Global exception handler (outermost — catches everything)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Structured request logging
app.UseSerilogRequestLogging();

// Security headers (before any response)
app.UseMiddleware<SecurityHeadersMiddleware>();

// Rate limiting
app.UseIpRateLimiting();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health checks
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { status = "healthy", service = "HoleriteSign API", version = "1.0.0" }));

// ── Seed SuperAdmin on startup ────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var encryption = scope.ServiceProvider.GetRequiredService<EncryptionService>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Apply pending migrations automatically
    await db.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully");

    var superEmail = config["SuperAdmin:Email"] ?? "superadmin@holeritesign.com";
    var superPassword = config["SuperAdmin:Password"] ?? "Super@123";

    var exists = await db.Admins.AnyAsync(a => a.Email == superEmail);
    if (!exists)
    {
        // Ensure a default plan exists
        var plan = await db.Plans.FirstOrDefaultAsync(p => p.Name == "enterprise");
        if (plan == null)
        {
            plan = new HoleriteSign.Core.Entities.Plan
            {
                Id = Guid.NewGuid(),
                Name = "enterprise",
                DisplayName = "Enterprise",
                MaxDocuments = -1,
                MaxEmployees = -1,
                PriceMonthly = 0,
                IsActive = true,
            };
            db.Plans.Add(plan);
        }

        var admin = new HoleriteSign.Core.Entities.Admin
        {
            Id = Guid.NewGuid(),
            Name = "Super Admin",
            Email = superEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(superPassword),
            CompanyName = "HoleriteSign Platform",
            Role = HoleriteSign.Core.Enums.AdminRole.SuperAdmin,
            PlanId = plan.Id,
            EmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Admins.Add(admin);
        await db.SaveChangesAsync();
        logger.LogInformation("SuperAdmin seeded: {Email}", superEmail);
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "SuperAdmin seed skipped (DB may not be available yet)");
}

app.Run();

// Expose Program class for WebApplicationFactory integration tests
public partial class Program { }