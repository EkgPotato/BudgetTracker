namespace Finance.Api.Validators;

internal static class InputLimits
{
    public const int NameLength = 100;
    public const int DescriptionLength = 500;
    public const decimal MaximumAmount = 999_999_999_999_999.99m;

    public static bool IsValidAmount(decimal amount) =>
        amount >= 0 && amount <= MaximumAmount && decimal.Round(amount, 2) == amount;

    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= NameLength;
}
