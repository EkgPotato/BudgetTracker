using System.Security.Claims;
using Finance.Api.Contracts.Auth;
using Microsoft.AspNetCore.Identity;

namespace Finance.Api.Managers;

public interface IAuthManager
{
    Task<IdentityResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<bool> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken);
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<AuthUserResponse?> GetCurrentUserAsync(ClaimsPrincipal principal);
    Task<AuthUserResponse?> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileRequest request, CancellationToken cancellationToken);
    Task<AuthUserResponse?> ChangeEmailAsync(ClaimsPrincipal principal, ChangeEmailRequest request, CancellationToken cancellationToken);
    Task<IdentityResult?> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequest request);
    Task<AuthUserResponse?> CompleteOnboardingAsync(ClaimsPrincipal principal, CompleteOnboardingRequest request, CancellationToken cancellationToken);
}
