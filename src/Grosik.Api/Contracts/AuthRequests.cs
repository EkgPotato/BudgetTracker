namespace Finance.Api.Contracts;

public sealed record RegisterRequest(string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record ConfirmEmailRequest(string UserId, string Token);
public sealed record UpdateProfileRequest(string DisplayName, bool OnboardingCompleted);
