using Finance.Domain.Entities;
using Xunit;

namespace Finance.Tests;

public sealed class DomainEntityDefaultsTests
{
    [Fact]
    public void NewAccount_HasPolishZlotyAsDefaultCurrency()
    {
        var account = new Account();

        Assert.Equal("PLN", account.Currency);
        Assert.True(account.IsActive);
    }

    [Fact]
    public void NewBudgetSpace_IsActive()
    {
        var budgetSpace = new BudgetSpace();

        Assert.True(budgetSpace.IsActive);
    }
}
