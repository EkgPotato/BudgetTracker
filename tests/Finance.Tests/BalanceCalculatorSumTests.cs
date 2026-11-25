using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Services;
using Xunit;

namespace Finance.Tests;

public sealed class BalanceCalculatorSumTests
{
    [Fact]
    public void Sum_AppliesSignsToAllLedgerEntries()
    {
        var transactions = new[]
        {
            new FinancialTransaction { Kind = TransactionKind.OpeningBalance, Amount = 100m },
            new FinancialTransaction { Kind = TransactionKind.Income, Amount = 50m },
            new FinancialTransaction { Kind = TransactionKind.Expense, Amount = 20m },
            new FinancialTransaction { Kind = TransactionKind.TransferDebit, Amount = 10m },
            new FinancialTransaction { Kind = TransactionKind.TransferCredit, Amount = 5m }
        };

        var balance = BalanceCalculator.Sum(transactions);

        Assert.Equal(125m, balance);
    }
}
