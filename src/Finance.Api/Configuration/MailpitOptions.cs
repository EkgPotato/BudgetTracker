namespace Finance.Api.Configuration;

public sealed class MailpitOptions
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string From { get; init; } = string.Empty;
    public string FrontendUrl { get; init; } = string.Empty;
    public bool UseSsl { get; init; }
    public int TimeoutSeconds { get; init; } = 15;
}
