using Finance.Localization;
using Xunit;

namespace Finance.Tests;

public sealed class ValidationMessagesTests
{
    [Fact]
    public void Get_ReturnsPolishValidationMessage()
    {
        var message = ValidationMessages.Get("EmailRequired");

        Assert.Equal("E-mail jest wymagany.", message);
    }
}
