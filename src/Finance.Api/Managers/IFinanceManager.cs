using Finance.Api.Contracts.Finance;

namespace Finance.Api.Managers;

public interface IFinanceManager
{
    Task<IReadOnlyList<BudgetSpaceResponse>> GetSpacesAsync(string ownerId, CancellationToken cancellationToken);
    Task<BudgetSpaceResponse> CreateSpaceAsync(string ownerId, BudgetSpaceRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AccountResponse>> GetAccountsAsync(string ownerId, CancellationToken cancellationToken);
    Task<AccountResponse?> CreateAccountAsync(string ownerId, AccountRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(string ownerId, CancellationToken cancellationToken);
    Task<CategoryResponse?> CreateCategoryAsync(string ownerId, CategoryRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TransactionResponse>> GetTransactionsAsync(string ownerId, CancellationToken cancellationToken);
    Task<TransactionResponse?> CreateTransactionAsync(string ownerId, TransactionRequest request, CancellationToken cancellationToken);
    Task<TransferResponse?> CreateTransferAsync(string ownerId, TransferRequest request, CancellationToken cancellationToken);
    Task<DashboardResponse> DashboardAsync(string ownerId, CancellationToken cancellationToken);
    Task<MonthlyReportResponse> MonthlyReportAsync(string ownerId, int year, int month, CancellationToken cancellationToken);
    Task<DateRangeReportResponse> ReportAsync(string ownerId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
