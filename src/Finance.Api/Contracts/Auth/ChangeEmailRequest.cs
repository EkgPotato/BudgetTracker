namespace Finance.Api.Contracts.Auth;

public sealed record ChangeEmailRequest(string? NewEmail, string? CurrentPassword);
