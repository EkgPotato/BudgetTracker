using System.Security.Claims;
using System.Text;
using System.Data;
using Finance.Api.Configuration;
using Finance.Api.Contracts.Auth;
using Finance.Api.Features;
using Finance.Api.Services;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Identity;
using Finance.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Finance.Api.Managers;

public sealed class AuthManager(
    UserManager<FinanceUser> users,
    FinanceDbContext database,
    IOnboardingService onboardingService,
    IJwtTokenManager tokenManager,
    IEmailSender emailSender,
    IOptions<MailpitOptions> mailpitOptions,
    ILogger<AuthManager> logger) : IAuthManager
{
    public async Task<IdentityResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email!.Trim();
        FinanceUser? user = null;
        IdentityResult result;
        if (database.Database.IsRelational())
        {
            await using var transaction = await database.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            (user, result) = await CreateUserAsync(normalizedEmail, request.Password!, cancellationToken);
            if (result.Succeeded) await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            (user, result) = await CreateUserAsync(normalizedEmail, request.Password!, cancellationToken);
        }
        if (!result.Succeeded) return result;

        try
        {
            await SendConfirmationAsync(user!, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Failed to send registration confirmation for user {UserId}.", user!.Id);
            await users.DeleteAsync(user);
            return IdentityResult.Failed(new IdentityError
            {
                Code = "EmailDeliveryFailed",
                Description = "Confirmation email delivery failed."
            });
        }
        return result;
    }

    public async Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await users.FindByIdAsync(request.UserId!);
        if (user is null) return false;

        try
        {
            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token!));
            return (await users.ConfirmEmailAsync(user, token)).Succeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public async Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(request.Email!.Trim());
        if (user is null || user.EmailConfirmed) return;

        try
        {
            await SendConfirmationAsync(user, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to resend confirmation for user {UserId}.", user.Id);
        }
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email!.Trim());
        if (user is null || await users.IsLockedOutAsync(user)) return null;
        if (!await users.CheckPasswordAsync(user, request.Password!))
        {
            await users.AccessFailedAsync(user);
            return null;
        }
        await users.ResetAccessFailedCountAsync(user);
        if (!user.EmailConfirmed) return null;

        return new LoginResponse(tokenManager.Create(user), ToResponse(user));
    }

    public async Task<AuthUserResponse?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var user = await users.GetUserAsync(principal);
        return user is null ? null : ToResponse(user);
    }

    public async Task<AuthUserResponse?> UpdateProfileAsync(
        ClaimsPrincipal principal,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return null;

        if (request.OnboardingCompleted &&
            !await database.Accounts.AnyAsync(account => account.BudgetSpace!.OwnerId == user.Id, cancellationToken))
            throw new InvalidOperationException(ValidationMessages.Get("OnboardingAccountRequired"));

        user.DisplayName = request.DisplayName!.Trim();
        user.OnboardingCompleted = request.OnboardingCompleted;
        var updateResult = await users.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException(ValidationMessages.Get("OperationFailed"));
        return ToResponse(user);
    }

    public async Task<AuthUserResponse?> ChangeEmailAsync(
        ClaimsPrincipal principal,
        ChangeEmailRequest request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return null;
        if (!await users.CheckPasswordAsync(user, request.CurrentPassword!))
            throw new InvalidOperationException(ValidationMessages.Get("CurrentPasswordInvalid"));

        var email = request.NewEmail!.Trim();
        if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(ValidationMessages.Get("EmailUnchanged"));
        var existingUser = await users.FindByEmailAsync(email);
        if (existingUser is not null && existingUser.Id != user.Id)
            throw new InvalidOperationException(ValidationMessages.Get("EmailAlreadyInUse"));

        var previousEmail = user.Email!;
        var previousUserName = user.UserName!;
        var wasEmailConfirmed = user.EmailConfirmed;
        try
        {
            EnsureSuccess(await users.SetEmailAsync(user, email));
            EnsureSuccess(await users.SetUserNameAsync(user, email));
            user.EmailConfirmed = false;
            EnsureSuccess(await users.UpdateAsync(user));
            await SendConfirmationAsync(user, cancellationToken);
            EnsureSuccess(await users.UpdateSecurityStampAsync(user));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await RestoreEmailAsync(user, previousEmail, previousUserName, wasEmailConfirmed);
            logger.LogWarning(exception, "Failed to change email address for user {UserId}.", user.Id);
            throw new InvalidOperationException(ValidationMessages.Get("EmailChangeFailed"));
        }
        return ToResponse(user);
    }

    public async Task<IdentityResult?> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequest request)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return null;
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword!, request.NewPassword!);
        if (result.Succeeded) EnsureSuccess(await users.UpdateSecurityStampAsync(user));
        return result;
    }

    public async Task<AuthUserResponse?> CompleteOnboardingAsync(
        ClaimsPrincipal principal,
        CompleteOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(principal);
        if (user is null) return null;
        await onboardingService.CompleteAsync(user, request, cancellationToken);
        var updateResult = await users.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException(ValidationMessages.Get("OperationFailed"));
        return ToResponse(user);
    }

    private static AuthUserResponse ToResponse(FinanceUser user)
    {
        return new AuthUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.IsAdmin,
            user.DisplayName,
            user.OnboardingCompleted);
    }

    private async Task<(FinanceUser User, IdentityResult Result)> CreateUserAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = new FinanceUser
        {
            UserName = email,
            Email = email,
            IsAdmin = !await users.Users.AnyAsync(cancellationToken)
        };
        return (user, await users.CreateAsync(user, password));
    }

    private async Task SendConfirmationAsync(FinanceUser user, CancellationToken cancellationToken)
    {
        var rawToken = await users.GenerateEmailConfirmationTokenAsync(user);
        var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));
        var link =
            $"{mailpitOptions.Value.FrontendUrl}/auth?confirmUserId={Uri.EscapeDataString(user.Id)}&confirmToken={Uri.EscapeDataString(token)}";
        await emailSender.SendConfirmationAsync(user.Email!, link, cancellationToken);
    }

    private async Task RestoreEmailAsync(FinanceUser user, string email, string userName, bool emailConfirmed)
    {
        EnsureSuccess(await users.SetEmailAsync(user, email));
        EnsureSuccess(await users.SetUserNameAsync(user, userName));
        user.EmailConfirmed = emailConfirmed;
        EnsureSuccess(await users.UpdateAsync(user));
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(ValidationMessages.Get("OperationFailed"));
    }
}
