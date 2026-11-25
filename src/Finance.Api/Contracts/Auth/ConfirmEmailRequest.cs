namespace Finance.Api.Contracts.Auth;

public sealed record ConfirmEmailRequest(string? UserId, string? Token);