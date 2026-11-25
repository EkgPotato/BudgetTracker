using Finance.Api.Contracts.Finance;
using Finance.Api.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Domain.Services;

namespace Finance.Api.Managers;

public sealed class FinanceManager(IFinanceDataService dataService) : IFinanceManager
{
    public async Task<IReadOnlyList<BudgetSpaceResponse>> GetSpacesAsync(string ownerId, CancellationToken token)
    {
        return (await dataService.GetSpacesAsync(ownerId, token))
            .Select(x => new BudgetSpaceResponse(x.Id, x.Name, x.IsActive)).ToList();
    }

    public async Task<BudgetSpaceResponse> CreateSpaceAsync(string ownerId, BudgetSpaceRequest request,
        CancellationToken token)
    {
        var x = await dataService.CreateSpaceAsync(ownerId, request, token);
        return new BudgetSpaceResponse(x.Id, x.Name, x.IsActive);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(string ownerId, CancellationToken token)
    {
        return (await dataService.GetAccountsAsync(ownerId, token)).Select(ToAccount).ToList();
    }

    public async Task<AccountResponse?> CreateAccountAsync(string ownerId, AccountRequest request,
        CancellationToken token)
    {
        var x = await dataService.CreateAccountAsync(ownerId, request, token);
        return x is null ? null : ToAccount(x);
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(string ownerId, CancellationToken token)
    {
        return (await dataService.GetCategoriesAsync(ownerId, token)).Select(x =>
            new CategoryResponse(x.Id, x.BudgetSpaceId, x.ParentCategoryId, x.Name, x.AppliesTo, x.SortOrder,
                x.IsActive)).ToList();
    }

    public async Task<CategoryResponse?> CreateCategoryAsync(string ownerId, CategoryRequest request,
        CancellationToken token)
    {
        var x = await dataService.CreateCategoryAsync(ownerId, request, token);
        return x is null
            ? null
            : new CategoryResponse(x.Id, x.BudgetSpaceId, x.ParentCategoryId, x.Name, x.AppliesTo, x.SortOrder,
                x.IsActive);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsAsync(string ownerId, CancellationToken token)
    {
        return (await dataService.GetTransactionsAsync(ownerId, token)).Select(ToTransaction).ToList();
    }

    public async Task<TransactionResponse?> CreateTransactionAsync(string ownerId, TransactionRequest request,
        CancellationToken token)
    {
        var x = await dataService.CreateTransactionAsync(ownerId, request, token);
        return x is null
            ? null
            : new TransactionResponse(x.Id, x.Kind, x.Amount, x.OccurredOn, x.Description, x.AccountId, string.Empty,
                x.CategoryId, null);
    }

    public async Task<TransferResponse?> CreateTransferAsync(string ownerId, TransferRequest request,
        CancellationToken token)
    {
        var x = await dataService.CreateTransferAsync(ownerId, request, token);
        return x is null
            ? null
            : new TransferResponse(x.Id, x.SourceAccountId, x.DestinationAccountId, x.Amount, x.OccurredOn,
                x.Description);
    }

    public async Task<DashboardResponse> DashboardAsync(string ownerId, CancellationToken token)
    {
        var accounts = await GetAccountsAsync(ownerId, token);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
        var currentMonth = await dataService.GetTransactionsAsync(ownerId, startOfMonth, endOfMonth, null, token);
        var recentTransactions = currentMonth.Take(8).Select(ToTransaction).ToList();
        return new DashboardResponse(
            accounts.Sum(account => account.Balance),
            currentMonth.Where(item => item.Kind == TransactionKind.Income).Sum(item => item.Amount),
            currentMonth.Where(item => item.Kind == TransactionKind.Expense).Sum(item => item.Amount),
            accounts,
            recentTransactions);
    }

    public async Task<MonthlyReportResponse> MonthlyReportAsync(string ownerId, int year, int month, CancellationToken token)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var report = await BuildReportAsync(ownerId, from, to, token);
        return new MonthlyReportResponse(year, month, report.Income, report.Expenses, report.Categories);
    }

    public async Task<DateRangeReportResponse> ReportAsync(string ownerId, DateOnly from, DateOnly to, CancellationToken token)
    {
        var report = await BuildReportAsync(ownerId, from, to, token);
        return new DateRangeReportResponse(from, to, report.Income, report.Expenses, report.Categories);
    }

    private static AccountResponse ToAccount(Account x)
    {
        return new AccountResponse(x.Id, x.Name, x.Type, x.Currency, x.IsActive, x.BudgetSpaceId,
            x.BudgetSpace?.Name ?? string.Empty,
            BalanceCalculator.Sum(x.Transactions));
    }

    private static TransactionResponse ToTransaction(FinancialTransaction x)
    {
        return new TransactionResponse(x.Id, x.Kind, x.Amount, x.OccurredOn, x.Description, x.AccountId,
            x.Account?.Name ?? string.Empty,
            x.CategoryId, x.Category?.Name);
    }

    private async Task<ReportSummary> BuildReportAsync(
        string ownerId,
        DateOnly from,
        DateOnly to,
        CancellationToken token)
    {
        var rows = (await dataService.GetTransactionsAsync(ownerId, from, to, null, token))
            .Where(item => item.Kind is TransactionKind.Income or TransactionKind.Expense)
            .ToList();
        var categories = rows
            .GroupBy(item => new { item.Kind, item.CategoryId, Category = item.Category?.Name })
            .Select(group => new ReportCategoryResponse(
                group.Key.Kind,
                group.Key.CategoryId,
                group.Key.Category,
                group.Sum(item => item.Amount),
                group.Count()))
            .ToList();
        return new ReportSummary(
            rows.Where(item => item.Kind == TransactionKind.Income).Sum(item => item.Amount),
            rows.Where(item => item.Kind == TransactionKind.Expense).Sum(item => item.Amount),
            categories);
    }

    private sealed record ReportSummary(
        decimal Income,
        decimal Expenses,
        IReadOnlyList<ReportCategoryResponse> Categories);
}
