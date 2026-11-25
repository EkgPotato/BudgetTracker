using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Finance.Api.Contracts.Finance;
using Finance.Api.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Finance.Tests;

public sealed class TransactionPagingTests
{
    [Fact]
    public async Task Page_filters_orders_and_excludes_other_owners()
    {
        await using var database = CreateDatabase();
        var first = new Account { Name = "First", BudgetSpace = new BudgetSpace { Name = "A", OwnerId = "owner-a" } };
        var other = new Account { Name = "Other", BudgetSpace = new BudgetSpace { Name = "B", OwnerId = "owner-b" } };
        var date = new DateOnly(2026, 11, 1);
        database.AddRange(first, other,
            new FinancialTransaction { Account = first, Kind = TransactionKind.Expense, Amount = 1m, OccurredOn = date },
            new FinancialTransaction { Account = first, Kind = TransactionKind.Expense, Amount = 2m, OccurredOn = date.AddDays(1) },
            new FinancialTransaction { Account = first, Kind = TransactionKind.Income, Amount = 3m, OccurredOn = date.AddDays(1) },
            new FinancialTransaction { Account = other, Kind = TransactionKind.Expense, Amount = 4m, OccurredOn = date.AddDays(1) });
        await database.SaveChangesAsync();

        var page = await new FinanceDataService(database).GetTransactionPageAsync("owner-a", new TransactionPageQuery
        {
            Kinds = [TransactionKind.Expense], From = date, To = date.AddDays(1), Offset = 0, Limit = 1
        }, CancellationToken.None);

        Assert.Equal(2, page.Total);
        Assert.Single(page.Items);
        Assert.Equal(2m, page.Items[0].Amount);
        Assert.Equal(1, page.Limit);
    }

    [Fact]
    public async Task Balances_and_report_include_only_correct_transaction_types()
    {
        await using var database = CreateDatabase();
        var account = new Account { Name = "Account", BudgetSpace = new BudgetSpace { Name = "Budget", OwnerId = "owner" } };
        var date = new DateOnly(2026, 11, 1);
        database.AddRange(account,
            new FinancialTransaction { Account = account, Kind = TransactionKind.Income, Amount = 100m, OccurredOn = date },
            new FinancialTransaction { Account = account, Kind = TransactionKind.Expense, Amount = 30m, OccurredOn = date },
            new FinancialTransaction { Account = account, Kind = TransactionKind.TransferDebit, Amount = 20m, OccurredOn = date });
        await database.SaveChangesAsync();

        var service = new FinanceDataService(database);
        var balances = await service.GetAccountBalancesAsync("owner", CancellationToken.None);
        var report = await service.GetReportCategoriesAsync("owner", date, date, CancellationToken.None);

        Assert.Equal(50m, Assert.Single(balances).Balance);
        Assert.Equal(2, report.Count);
        Assert.Equal(100m, report.Single(item => item.Kind == TransactionKind.Income).Total);
        Assert.Equal(30m, report.Single(item => item.Kind == TransactionKind.Expense).Total);
    }

    private static FinanceDbContext CreateDatabase() => new(new DbContextOptionsBuilder<FinanceDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
