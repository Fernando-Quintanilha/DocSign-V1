using FluentAssertions;
using FluentValidation.TestHelper;
using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Validators;

namespace HoleriteSign.Tests.Validators;

public class ValidatorTests
{
    // ── RegisterRequest ───────────────────────────────────

    [Fact]
    public void Register_ValidRequest_NoErrors()
    {
        var validator = new RegisterRequestValidator();
        var req = new RegisterRequest("João", "joao@test.com", "Senha@123", "Corp LTDA");

        var result = validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Register_EmptyName_HasError()
    {
        var validator = new RegisterRequestValidator();
        var req = new RegisterRequest("", "joao@test.com", "Senha@123", "Corp");

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Register_InvalidEmail_HasError()
    {
        var validator = new RegisterRequestValidator();
        var req = new RegisterRequest("João", "not-an-email", "Senha@123", "Corp");

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short")]           // Too short
    [InlineData("nouppercase1!")]   // No uppercase
    [InlineData("NOLOWERCASE1!")]   // No lowercase
    [InlineData("NoSpecialChar1")]  // No special char
    [InlineData("NoNumber@abc")]    // No number
    public void Register_WeakPassword_HasError(string password)
    {
        var validator = new RegisterRequestValidator();
        var req = new RegisterRequest("João", "j@t.com", password, "Corp");

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Register_EmptyCompanyName_HasError()
    {
        var validator = new RegisterRequestValidator();
        var req = new RegisterRequest("João", "j@t.com", "Senha@123", "");

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.CompanyName);
    }

    // ── LoginRequest ──────────────────────────────────────

    [Fact]
    public void Login_ValidRequest_NoErrors()
    {
        var validator = new LoginRequestValidator();
        var req = new LoginRequest("joao@test.com", "Senha@123");

        var result = validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Login_EmptyEmail_HasError()
    {
        var validator = new LoginRequestValidator();
        var req = new LoginRequest("", "Senha@123");

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // ── CreateEmployeeRequest ─────────────────────────────

    [Fact]
    public void CreateEmployee_ValidWithEmail_NoErrors()
    {
        var validator = new CreateEmployeeRequestValidator();
        var req = new CreateEmployeeRequest("Maria", "maria@emp.com", null, "12345678901", "1990-01-15");

        var result = validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateEmployee_NoEmailNorWhatsApp_HasError()
    {
        var validator = new CreateEmployeeRequestValidator();
        var req = new CreateEmployeeRequest("Maria", null, null, null, null);

        // The custom rule should trigger
        var result = validator.TestValidate(req);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("email ou WhatsApp"));
    }

    [Fact]
    public void CreateEmployee_InvalidCpf_HasError()
    {
        var validator = new CreateEmployeeRequestValidator();
        var req = new CreateEmployeeRequest("Maria", "m@e.com", null, "123", null);

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }

    [Fact]
    public void CreateEmployee_InvalidBirthDate_HasError()
    {
        var validator = new CreateEmployeeRequestValidator();
        var req = new CreateEmployeeRequest("Maria", "m@e.com", null, null, "2026/01/15");

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    // ── CreatePayPeriodRequest ────────────────────────────

    [Fact]
    public void CreatePayPeriod_Valid_NoErrors()
    {
        var validator = new CreatePayPeriodRequestValidator();
        var req = new CreatePayPeriodRequest(2026, 2, "Fevereiro 2026");

        var result = validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreatePayPeriod_InvalidMonth_HasError()
    {
        var validator = new CreatePayPeriodRequestValidator();
        var req = new CreatePayPeriodRequest(2026, 13, null);

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void CreatePayPeriod_YearTooLow_HasError()
    {
        var validator = new CreatePayPeriodRequestValidator();
        var req = new CreatePayPeriodRequest(2019, 1, null);

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    // ── SignDocumentRequest ───────────────────────────────

    [Fact]
    public void SignDocument_ValidRequest_NoErrors()
    {
        var validator = new SignDocumentRequestValidator();
        var photo = Convert.ToBase64String(new byte[2048]);
        var req = new SignDocumentRequest(photo, "image/jpeg", true, null);

        var result = validator.TestValidate(req);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SignDocument_NoConsent_HasError()
    {
        var validator = new SignDocumentRequestValidator();
        var req = new SignDocumentRequest("photo", "image/jpeg", false, null);

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.ConsentGiven);
    }

    [Fact]
    public void SignDocument_InvalidMimeType_HasError()
    {
        var validator = new SignDocumentRequestValidator();
        var req = new SignDocumentRequest("photo", "image/bmp", true, null);

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.PhotoMimeType);
    }

    [Fact]
    public void SignDocument_EmptyPhoto_HasError()
    {
        var validator = new SignDocumentRequestValidator();
        var req = new SignDocumentRequest("", "image/jpeg", true, null);

        var result = validator.TestValidate(req);

        result.ShouldHaveValidationErrorFor(x => x.PhotoBase64);
    }
}
