using Finance.Domain.Enums;
using Finance.Domain.Services;
using Xunit;

namespace Finance.Tests;

public sealed class BalanceCalculatorTests
{
    [Theory]
    [InlineData(TransactionKind.Income, 120, 120)]
    [InlineData(TransactionKind.OpeningBalance, 120, 120)]
    [InlineData(TransactionKind.Expense, 120, -120)]
    [InlineData(TransactionKind.TransferDebit, 120, -120)]
    [InlineData(TransactionKind.TransferCredit, 120, 120)]
    public void Apply_UsesCorrectSign(TransactionKind kind, int amount, int expected)
    {
        Assert.Equal(expected, BalanceCalculator.Apply(kind, amount));
    }
}