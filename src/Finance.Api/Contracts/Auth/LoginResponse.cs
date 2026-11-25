namespace Finance.Api.Contracts.Auth;

public sealed record LoginResponse(string AccessToken, AuthUserResponse User);