namespace Finance.Api.Contracts.Auth;

public sealed record UpdateProfileRequest(string? DisplayName, bool OnboardingCompleted);