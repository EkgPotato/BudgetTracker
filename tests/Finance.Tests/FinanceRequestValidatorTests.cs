using Finance.Api.Contracts.Auth;
using Finance.Api.Contracts.Finance;
using Finance.Api.Validators;
using Finance.Domain.Enums;
using System;
using Xunit;

namespace Finance.Tests;

public sealed class FinanceRequestValidatorTests
{
    private readonly FinanceRequestValidator validator = new();

    [Theory]
    [InlineData("0.001")]
    [InlineData("1000000000000000")]
    public void Rejects_invalid_monetary_precision_or_range(string value)
    {
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var request = new TransactionRequest(Guid.NewGuid(), Guid.NewGuid(), TransactionKind.Expense, amount, new DateOnly(2026, 1, 1), "test");
        Assert.NotEmpty(validator.Validate(request));
    }

    [Fact]
    public void Accepts_valid_amount_and_rejects_oversized_description()
    {
        var request = new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), 12.34m, new DateOnly(2026, 1, 1), "ok");
        Assert.Empty(validator.Validate(request));
        Assert.NotEmpty(validator.Validate(request with { Description = new string('x', 501) }));
    }

    [Fact]
    public void Rejects_oversized_names_and_fractional_opening_balance()
    {
        Assert.NotEmpty(validator.Validate(new BudgetSpaceRequest(new string('x', 101))));
        var onboarding = new CompleteOnboardingRequest("Name", "Budget", "Account", AccountType.Bank, 0.001m);
        Assert.NotEmpty(new AuthRequestValidator().Validate(onboarding));
    }
}
