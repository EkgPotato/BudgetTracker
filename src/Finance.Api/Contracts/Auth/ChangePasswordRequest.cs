namespace Finance.Api.Contracts.Auth;

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
