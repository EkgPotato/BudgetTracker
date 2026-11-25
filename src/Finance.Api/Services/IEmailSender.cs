namespace Finance.Api.Services;

public interface IEmailSender
{
    Task SendConfirmationAsync(string recipient, string link, CancellationToken cancellationToken);
}
