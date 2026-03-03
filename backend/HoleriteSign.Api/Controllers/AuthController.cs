using HoleriteSign.Api.DTOs;
using HoleriteSign.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HoleriteSign.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    private Guid GetAdminId()
    {
        var sub = User.FindFirstValue("sub")
              ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(sub!);
    }

    /// <summary>POST /api/auth/register — Create a new admin account.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Created("", result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/auth/login — Authenticate and return JWT + refresh token.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/auth/refresh — Get a new JWT using a refresh token.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/auth/logout — Invalidate the refresh token.</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(GetAdminId());
        return Ok(new { message = "Sessão encerrada." });
    }

    /// <summary>POST /api/auth/send-verification — Resend email verification link.</summary>
    [HttpPost("send-verification")]
    [Authorize]
    public async Task<IActionResult> SendVerification()
    {
        try
        {
            await _authService.SendVerificationEmailAsync(GetAdminId());
            return Ok(new { message = "E-mail de verificação enviado." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/auth/verify-email — Verify email with token.</summary>
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            await _authService.VerifyEmailAsync(request);
            return Ok(new { message = "E-mail verificado com sucesso!" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/auth/forgot-password — Send password reset email.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            await _authService.ForgotPasswordAsync(request);
            // Always return success to prevent email enumeration
            return Ok(new { message = "Se o e-mail estiver cadastrado, você receberá um link de redefinição." });
        }
        catch
        {
            return Ok(new { message = "Se o e-mail estiver cadastrado, você receberá um link de redefinição." });
        }
    }

    /// <summary>POST /api/auth/reset-password — Reset password with token.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            await _authService.ResetPasswordAsync(request);
            return Ok(new { message = "Senha redefinida com sucesso!" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>GET /api/auth/profile — Get current admin profile.</summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _authService.GetProfileAsync(GetAdminId());
        return Ok(profile);
    }

    /// <summary>PUT /api/auth/profile — Update admin name/company.</summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var profile = await _authService.UpdateProfileAsync(GetAdminId(), request);
            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>POST /api/auth/change-password — Change password (authenticated).</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            await _authService.ChangePasswordAsync(GetAdminId(), request);
            return Ok(new { message = "Senha alterada com sucesso." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
