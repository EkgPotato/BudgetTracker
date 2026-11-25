using Finance.Api.Contracts.Finance;
using Finance.Domain.Entities;

namespace Finance.Api.Services;

public interface IFinanceDataService
{
    Task<List<BudgetSpace>> GetSpacesAsync(string ownerId, CancellationToken cancellationToken);
    Task<BudgetSpace> CreateSpaceAsync(string ownerId, BudgetSpaceRequest request, CancellationToken cancellationToken);
    Task<List<Account>> GetAccountsAsync(string ownerId, CancellationToken cancellationToken);
    Task<Account?> CreateAccountAsync(string ownerId, AccountRequest request, CancellationToken cancellationToken);
    Task<List<Category>> GetCategoriesAsync(string ownerId, CancellationToken cancellationToken);
    Task<Category?> CreateCategoryAsync(string ownerId, CategoryRequest request, CancellationToken cancellationToken);
    Task<List<FinancialTransaction>> GetTransactionsAsync(string ownerId, CancellationToken cancellationToken);
    async Task<List<FinancialTransaction>> GetTransactionsAsync(
        string ownerId,
        DateOnly? from,
        DateOnly? to,
        int? take,
        CancellationToken cancellationToken)
    {
        var transactions = await GetTransactionsAsync(ownerId, cancellationToken);
        var query = transactions.AsEnumerable();
        if (from is not null) query = query.Where(item => item.OccurredOn >= from.Value);
        if (to is not null) query = query.Where(item => item.OccurredOn <= to.Value);
        if (take is not null) query = query.Take(take.Value);
        return query.ToList();
    }
    Task<FinancialTransaction?> CreateTransactionAsync(string ownerId, TransactionRequest request, CancellationToken cancellationToken);
    Task<Transfer?> CreateTransferAsync(string ownerId, TransferRequest request, CancellationToken cancellationToken);
}
