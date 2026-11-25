namespace Finance.Api.Contracts.Auth;

public sealed record AuthUserResponse(
    string Id,
    string Email,
    bool IsAdmin,
    string? DisplayName,
    bool OnboardingCompleted);