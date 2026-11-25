using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Finance.Api.Contracts.Finance;
using Finance.Api.Managers;
using Finance.Api.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Xunit;

namespace Finance.Tests;

public sealed class MonthlyReportTests
{
    [Fact]
    public async Task MonthlyReport_ExcludesOpeningBalancesFromIncomeExpensesAndCategories()
    {
        var date = new DateOnly(2026, 11, 1);
        var dataService = new StubFinanceDataService(
        [
            new FinancialTransaction { Kind = TransactionKind.OpeningBalance, Amount = 15_000m, OccurredOn = date },
            new FinancialTransaction { Kind = TransactionKind.Income, Amount = 8_000m, OccurredOn = date, Category = new Category { Name = "Pensja" } },
            new FinancialTransaction { Kind = TransactionKind.Expense, Amount = 1_200m, OccurredOn = date, Category = new Category { Name = "Mieszkanie" } }
        ]);

        var result = await new FinanceManager(dataService).MonthlyReportAsync("user", 2026, 11, CancellationToken.None);
        Assert.Equal(8_000m, result.Income);
        Assert.Equal(1_200m, result.Expenses);
        Assert.Equal(2, result.Categories.Count);
    }

    [Fact]
    public async Task DateRangeReport_UsesInclusiveBoundariesAndExcludesTransfers()
    {
        var from = new DateOnly(2026, 11, 10);
        var dataService = new StubFinanceDataService(
        [
            new FinancialTransaction { Kind = TransactionKind.Expense, Amount = 10m, OccurredOn = from },
            new FinancialTransaction { Kind = TransactionKind.Income, Amount = 25m, OccurredOn = from.AddDays(1) },
            new FinancialTransaction { Kind = TransactionKind.TransferCredit, Amount = 99m, OccurredOn = from.AddDays(1) },
            new FinancialTransaction { Kind = TransactionKind.Expense, Amount = 30m, OccurredOn = from.AddDays(2) }
        ]);

        var result = await new FinanceManager(dataService).ReportAsync(
            "user", from, from.AddDays(1), CancellationToken.None);

        Assert.Equal(25m, result.Income);
        Assert.Equal(10m, result.Expenses);
        Assert.Equal(2, result.Categories.Count);
    }

    private sealed class StubFinanceDataService(IReadOnlyList<FinancialTransaction> transactions) : IFinanceDataService
    {
        public Task<List<FinancialTransaction>> GetTransactionsAsync(string ownerId, CancellationToken cancellationToken) => Task.FromResult(transactions.ToList());
        public Task<List<BudgetSpace>> GetSpacesAsync(string ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<BudgetSpace> CreateSpaceAsync(string ownerId, BudgetSpaceRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<List<Account>> GetAccountsAsync(string ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Account?> CreateAccountAsync(string ownerId, AccountRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<List<Category>> GetCategoriesAsync(string ownerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Category?> CreateCategoryAsync(string ownerId, CategoryRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FinancialTransaction?> CreateTransactionAsync(string ownerId, TransactionRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Transfer?> CreateTransferAsync(string ownerId, TransferRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
