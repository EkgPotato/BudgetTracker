namespace Finance.Api.Contracts.Finance;

public sealed record DashboardResponse(
    decimal TotalBalance,
    decimal IncomeThisMonth,
    decimal ExpenseThisMonth,
    IReadOnlyList<AccountResponse> Accounts,
    IReadOnlyList<TransactionResponse> RecentTransactions);
