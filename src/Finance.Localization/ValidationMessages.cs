using System.Globalization;
using System.Resources;

namespace Finance.Localization;

public static class ValidationMessages
{
    private static readonly ResourceManager ResourceManager = new(
        "Finance.Localization.Resources.ValidationMessages",
        typeof(ValidationMessages).Assembly);

    public static string Get(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
}
