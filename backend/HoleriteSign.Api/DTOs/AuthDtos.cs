namespace HoleriteSign.Api.DTOs;

public record RegisterRequest(
    string Name,
    string Email,
    string Password,
    string CompanyName
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string RefreshToken,
    AdminDto Admin
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record RefreshTokenResponse(
    string Token,
    string RefreshToken
);

public record ForgotPasswordRequest(
    string Email
);

public record ResetPasswordRequest(
    string Token,
    string NewPassword
);

public record VerifyEmailRequest(
    string Token
);

public record AdminDto(
    Guid Id,
    string Name,
    string Email,
    string CompanyName,
    string Role,
    string PlanName,
    bool EmailVerified
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record UpdateProfileRequest(
    string Name,
    string CompanyName
);
