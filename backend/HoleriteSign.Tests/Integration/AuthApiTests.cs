using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Core.Entities;
using HoleriteSign.Core.Enums;
using HoleriteSign.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HoleriteSign.Tests.Integration;

public class AuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    private static readonly string _dbName = "IntegrationTestDb_Auth_" + Guid.NewGuid();

    public AuthApiTests(WebApplicationFactory<Program> factory)
    {
        var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove real DB registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                // Use a shared InMemory DB name for all tests in this class
                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });
        });

        _client = customFactory.CreateClient();

        // Seed using the app's real service provider
        using var scope = customFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Plans.Any(p => p.Name == "free"))
        {
            db.Plans.Add(new Plan
            {
                Id = Guid.NewGuid(),
                Name = "free",
                DisplayName = "Plano Gratuito",
                MaxDocuments = 10,
                MaxEmployees = 5,
                PriceMonthly = 0,
                IsActive = true,
            });
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task Register_ValidData_Returns201()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var req = new RegisterRequest("Admin Test", $"admin_{unique}@test.com", "Senha@123", "Test Corp");

        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.Admin.Email.Should().Be($"admin_{unique}@test.com");
    }

    [Fact]
    public async Task Register_InvalidData_Returns400WithValidationErrors()
    {
        var req = new { name = "", email = "bad", password = "x", companyName = "" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Nome");
        body.Should().Contain("Email");
        body.Should().Contain("Senha");
    }

    [Fact]
    public async Task Login_WrongCredentials_Returns401()
    {
        var req = new LoginRequest("wrong@test.com", "WrongPass@1");

        var response = await _client.PostAsJsonAsync("/api/auth/login", req);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AfterRegister_ReturnsToken()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        // Register first
        var regReq = new RegisterRequest("Login Admin", $"login_{unique}@test.com", "Senha@123", "Corp");
        var regResp = await _client.PostAsJsonAsync("/api/auth/register", regReq);
        regResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Then login
        var loginReq = new LoginRequest($"login_{unique}@test.com", "Senha@123");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("healthy");
    }

    [Fact]
    public async Task SecurityHeaders_ArePresent()
    {
        var response = await _client.GetAsync("/");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("X-XSS-Protection");
        response.Headers.Should().ContainKey("Referrer-Policy");
    }
}
