using Finance.Api.Contracts.Auth;
using Finance.Api.Managers;
using Finance.Api.Validators;
using Finance.Api.Features;
using Finance.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Finance.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthManager manager, AuthRequestValidator validator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        var result = await manager.RegisterAsync(request, cancellationToken);
        return result.Succeeded
            ? Accepted(new { message = ValidationMessages.Get("ConfirmationEmailSent") })
            : ValidationProblem(new ValidationProblemDetails(
                IdentityErrorLocalizer.ToValidationErrors(result.Errors)));
    }

    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        return await manager.ConfirmEmailAsync(request)
            ? Ok(new { message = ValidationMessages.Get("EmailConfirmed") })
            : BadRequest(new { message = ValidationMessages.Get("ConfirmationLinkInvalid") });
    }

    [AllowAnonymous]
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        await manager.ResendConfirmationAsync(request, cancellationToken);
        return Accepted(new { message = ValidationMessages.Get("ConfirmationEmailResent") });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        var response = await manager.LoginAsync(request);
        return response is null ? Unauthorized() : Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserResponse>> Me()
    {
        var response = await manager.GetCurrentUserAsync(User);
        return response is null ? Unauthorized() : Ok(response);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<AuthUserResponse>> UpdateProfile(UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        try
        {
            var response = await manager.UpdateProfileAsync(User, request, cancellationToken);
            return response is null ? Unauthorized() : Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["profile"] = [exception.Message] }));
        }
    }

    [Authorize]
    [HttpPut("email")]
    public async Task<ActionResult<AuthUserResponse>> ChangeEmail(ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        try
        {
            var response = await manager.ChangeEmailAsync(User, request, cancellationToken);
            return response is null ? Unauthorized() : Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["email"] = [exception.Message] }));
        }
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        var result = await manager.ChangePasswordAsync(User, request);
        if (result is null) return Unauthorized();
        return result.Succeeded
            ? NoContent()
            : ValidationProblem(new ValidationProblemDetails(
                IdentityErrorLocalizer.ToValidationErrors(result.Errors)));
    }

    [Authorize]
    [HttpPost("onboarding")]
    public async Task<ActionResult<AuthUserResponse>> CompleteOnboarding(CompleteOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request);
        if (errors.Count > 0) return ValidationProblem(new ValidationProblemDetails(errors));
        try
        {
            var response = await manager.CompleteOnboardingAsync(User, request, cancellationToken);
            return response is null ? Unauthorized() : Ok(response);
        }
        catch (InvalidOperationException exception)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { ["onboarding"] = [exception.Message] }));
        }
    }
}
