using Finance.Api.Contracts.Auth;
using Finance.Localization;

namespace Finance.Api.Validators;

public sealed class AuthRequestValidator
{
    public Dictionary<string, string[]> Validate(RegisterRequest request)
    {
        return ValidateCredentials(request.Email, request.Password);
    }

    public Dictionary<string, string[]> Validate(LoginRequest request)
    {
        return ValidateCredentials(request.Email, request.Password);
    }

    public Dictionary<string, string[]> Validate(ConfirmEmailRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.UserId))
            errors["userId"] = [ValidationMessages.Get("UserIdRequired")];
        if (string.IsNullOrWhiteSpace(request.Token))
            errors["token"] = [ValidationMessages.Get("TokenRequired")];
        return errors;
    }

    public Dictionary<string, string[]> Validate(ResendConfirmationRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Email) ||
               !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Email.Trim())
            ? new Dictionary<string, string[]> { ["email"] = ["Podaj prawidłowy adres e-mail."] }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(UpdateProfileRequest request)
    {
        return string.IsNullOrWhiteSpace(request.DisplayName)
            ? new Dictionary<string, string[]> { ["displayName"] = [ValidationMessages.Get("DisplayNameRequired")] }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(ChangeEmailRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.NewEmail))
            errors["newEmail"] = [ValidationMessages.Get("EmailRequired")];
        else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.NewEmail.Trim()))
            errors["newEmail"] = ["Podaj prawidłowy adres e-mail."];
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            errors["currentPassword"] = [ValidationMessages.Get("PasswordRequired")];
        return errors;
    }

    public Dictionary<string, string[]> Validate(ChangePasswordRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            errors["currentPassword"] = [ValidationMessages.Get("PasswordRequired")];
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            errors["newPassword"] = [ValidationMessages.Get("PasswordRequired")];
        else if (request.NewPassword.Length < 8)
            errors["newPassword"] = ["Nowe hasło musi mieć co najmniej 8 znaków."];
        return errors;
    }

    public Dictionary<string, string[]> Validate(CompleteOnboardingRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            errors["displayName"] = [ValidationMessages.Get("DisplayNameRequired")];
        if (string.IsNullOrWhiteSpace(request.BudgetSpaceName))
            errors["budgetSpaceName"] = [ValidationMessages.Get("BudgetSpaceNameRequired")];
        if (string.IsNullOrWhiteSpace(request.AccountName))
            errors["accountName"] = [ValidationMessages.Get("AccountNameRequired")];
        if (request.OpeningBalance < 0)
            errors["openingBalance"] = [ValidationMessages.Get("OpeningBalanceNonNegative")];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateCredentials(string? email, string? password)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(email))
            errors["email"] = [ValidationMessages.Get("EmailRequired")];
        if (string.IsNullOrWhiteSpace(password))
            errors["password"] = [ValidationMessages.Get("PasswordRequired")];
        return errors;
    }
}
