using System.Globalization;
using Finance.Localization;
using Xunit;

namespace Finance.Tests;

public sealed class ValidationMessagesTests
{
    [Theory]
    [InlineData("en", "Email is required.")]
    [InlineData("pl", "Adres e-mail jest wymagany.")]
    public void Get_UsesCurrentUiCulture(string cultureName, string expectedMessage)
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            var message = ValidationMessages.Get("EmailRequired");

            Assert.Equal(expectedMessage, message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
