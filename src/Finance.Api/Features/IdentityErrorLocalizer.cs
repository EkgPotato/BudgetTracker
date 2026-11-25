using Finance.Localization;
using Microsoft.AspNetCore.Identity;

namespace Finance.Api.Features;

public static class IdentityErrorLocalizer
{
    public static string GetMessage(IdentityError error)
    {
        return error.Code switch
        {
            "PasswordTooShort" => ValidationMessages.Format("PasswordTooShort", 8),
            "PasswordRequiresUpper" => ValidationMessages.Get("PasswordRequiresUpper"),
            "PasswordRequiresLower" => ValidationMessages.Get("PasswordRequiresLower"),
            "PasswordRequiresDigit" => ValidationMessages.Get("PasswordRequiresDigit"),
            "PasswordRequiresNonAlphanumeric" => ValidationMessages.Get("PasswordRequiresNonAlphanumeric"),
            "DuplicateEmail" or "DuplicateUserName" => ValidationMessages.Get("EmailAlreadyInUse"),
            "EmailDeliveryFailed" => ValidationMessages.Get("EmailDeliveryFailed"),
            _ => ValidationMessages.Get("OperationFailed")
        };
    }

    public static Dictionary<string, string[]> ToValidationErrors(IEnumerable<IdentityError> errors)
    {
        return errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(GetMessage).ToArray());
    }
}
