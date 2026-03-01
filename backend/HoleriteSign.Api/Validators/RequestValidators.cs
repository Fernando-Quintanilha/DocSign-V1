using FluentValidation;
using HoleriteSign.Api.DTOs;

namespace HoleriteSign.Api.Validators;

// ── Auth ──────────────────────────────────────────────────

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(255).WithMessage("Nome deve ter no máximo 255 caracteres.")
            .Matches(@"^[\p{L}\s.\-']+$").WithMessage("Nome contém caracteres inválidos.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(255).WithMessage("Email deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres.")
            .MaximumLength(128).WithMessage("Senha deve ter no máximo 128 caracteres.")
            .Matches(@"[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula.")
            .Matches(@"[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula.")
            .Matches(@"[0-9]").WithMessage("Senha deve conter pelo menos um número.")
            .Matches(@"[\W_]").WithMessage("Senha deve conter pelo menos um caractere especial.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Nome da empresa é obrigatório.")
            .MaximumLength(255).WithMessage("Nome da empresa deve ter no máximo 255 caracteres.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MaximumLength(128).WithMessage("Senha inválida.");
    }
}

// ── Employee ──────────────────────────────────────────────

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequest>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(255).WithMessage("Nome deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.WhatsApp)
            .Matches(@"^\+?\d{10,15}$").WithMessage("WhatsApp deve estar no formato E.164 (ex: +5511999999999).")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp));

        RuleFor(x => x.Cpf)
            .Must(cpf => {
                var digits = new string(cpf.Where(char.IsDigit).ToArray());
                return digits.Length == 11;
            }).WithMessage("CPF deve conter exatamente 11 dígitos numéricos.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf));

        RuleFor(x => x.BirthDate)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Data de nascimento deve estar no formato yyyy-MM-dd.")
            .When(x => !string.IsNullOrWhiteSpace(x.BirthDate));

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.WhatsApp))
            .WithMessage("Informe pelo menos email ou WhatsApp.");
    }
}

public class UpdateEmployeeRequestValidator : AbstractValidator<UpdateEmployeeRequest>
{
    public UpdateEmployeeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(255).WithMessage("Nome deve ter no máximo 255 caracteres.");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.WhatsApp)
            .Matches(@"^\+?\d{10,15}$").WithMessage("WhatsApp inválido.")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp));

        RuleFor(x => x.Cpf)
            .Must(cpf => {
                var digits = new string(cpf.Where(char.IsDigit).ToArray());
                return digits.Length == 11;
            }).WithMessage("CPF deve conter exatamente 11 dígitos.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf));

        RuleFor(x => x.BirthDate)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Data de nascimento inválida.")
            .When(x => !string.IsNullOrWhiteSpace(x.BirthDate));
    }
}

// ── Signing ───────────────────────────────────────────────

public class VerifyIdentityRequestValidator : AbstractValidator<VerifyIdentityRequest>
{
    public VerifyIdentityRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Cpf) || !string.IsNullOrWhiteSpace(x.BirthDate))
            .WithMessage("Informe CPF ou data de nascimento para verificação.");

        RuleFor(x => x.Cpf)
            .Matches(@"^[\d.\-]+$").WithMessage("CPF contém caracteres inválidos.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cpf));

        RuleFor(x => x.BirthDate)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Data de nascimento inválida.")
            .When(x => !string.IsNullOrWhiteSpace(x.BirthDate));
    }
}

public class SignDocumentRequestValidator : AbstractValidator<SignDocumentRequest>
{
    public SignDocumentRequestValidator()
    {
        RuleFor(x => x.PhotoBase64)
            .NotEmpty().WithMessage("Foto é obrigatória.")
            .Must(x => x.Length <= 10 * 1024 * 1024) // ~7.5 MB image
            .WithMessage("Imagem da selfie muito grande (máximo 7.5 MB).");

        RuleFor(x => x.PhotoMimeType)
            .NotEmpty().WithMessage("Tipo da imagem é obrigatório.")
            .Must(x => x is "image/jpeg" or "image/png" or "image/webp")
            .WithMessage("Tipo de imagem inválido. Use JPEG, PNG ou WebP.");

        RuleFor(x => x.ConsentGiven)
            .Equal(true).WithMessage("É necessário aceitar os termos para assinar.");
    }
}

// ── Pay Period ────────────────────────────────────────────

public class CreatePayPeriodRequestValidator : AbstractValidator<CreatePayPeriodRequest>
{
    public CreatePayPeriodRequestValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2020, 2100).WithMessage("Ano inválido.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12).WithMessage("Mês deve estar entre 1 e 12.");

        RuleFor(x => x.Label)
            .MaximumLength(50).WithMessage("Label deve ter no máximo 50 caracteres.")
            .When(x => x.Label != null);
    }
}
