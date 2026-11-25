using Finance.Api.Contracts.Finance;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Finance.Api.Services;

public sealed class FinanceDataService(FinanceDbContext database) : IFinanceDataService
{
    public Task<List<BudgetSpace>> GetSpacesAsync(string ownerId, CancellationToken token)
    {
        return database.BudgetSpaces.AsNoTracking().Where(space => space.OwnerId == ownerId)
            .OrderBy(space => space.Name).ToListAsync(token);
    }

    public async Task<BudgetSpace> CreateSpaceAsync(string ownerId, BudgetSpaceRequest request, CancellationToken token)
    {
        var space = new BudgetSpace { OwnerId = ownerId, Name = request.Name!.Trim() };
        database.BudgetSpaces.Add(space);
        await database.SaveChangesAsync(token);
        return space;
    }

    public Task<List<Account>> GetAccountsAsync(string ownerId, CancellationToken token)
    {
        return database.Accounts.AsNoTracking().Include(account => account.BudgetSpace)
            .Include(account => account.Transactions).Where(account => account.BudgetSpace!.OwnerId == ownerId)
            .OrderBy(account => account.Name).ToListAsync(token);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAccountBalancesAsync(string ownerId, CancellationToken token)
    {
        return await database.Accounts.AsNoTracking()
            .Where(account => account.BudgetSpace!.OwnerId == ownerId)
            .OrderBy(account => account.Name)
            .Select(account => new AccountResponse(
                account.Id, account.Name, account.Type, account.Currency, account.IsActive, account.BudgetSpaceId,
                account.BudgetSpace!.Name,
                account.Transactions.Sum(item => (decimal?)(
                    item.Kind == TransactionKind.Expense || item.Kind == TransactionKind.TransferDebit
                        ? -item.Amount : item.Amount)) ?? 0m))
            .ToListAsync(token);
    }

    public async Task<Account?> CreateAccountAsync(string ownerId, AccountRequest request, CancellationToken token)
    {
        var space = await database.BudgetSpaces.SingleOrDefaultAsync(
            item => item.Id == request.BudgetSpaceId && item.OwnerId == ownerId, token);
        if (space is null) return null;
        var account = new Account { BudgetSpaceId = space.Id, Name = request.Name!.Trim(), Type = request.Type };
        database.Accounts.Add(account);
        if (request.OpeningBalance > 0)
            database.Transactions.Add(new FinancialTransaction
            {
                Account = account, Amount = request.OpeningBalance.Value, Kind = TransactionKind.OpeningBalance,
                OccurredOn = request.OpeningBalanceDate ?? DateOnly.FromDateTime(DateTime.Today),
                Description = "Opening balance"
            });
        await database.SaveChangesAsync(token);
        return account;
    }

    public Task<List<Category>> GetCategoriesAsync(string ownerId, CancellationToken token)
    {
        return database.Categories.AsNoTracking().Include(category => category.BudgetSpace)
            .Where(category => category.BudgetSpace!.OwnerId == ownerId).OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name).ToListAsync(token);
    }

    public async Task<Category?> CreateCategoryAsync(string ownerId, CategoryRequest request, CancellationToken token)
    {
        var validSpace =
            await database.BudgetSpaces.AnyAsync(space => space.Id == request.BudgetSpaceId && space.OwnerId == ownerId,
                token);
        if (!validSpace) return null;
        if (request.ParentCategoryId is not null)
        {
            var parent = await database.Categories.SingleOrDefaultAsync(category =>
                category.Id == request.ParentCategoryId &&
                category.BudgetSpaceId == request.BudgetSpaceId &&
                category.AppliesTo == request.AppliesTo,
                token);
            if (parent is null) return null;
        }
        var category = new Category
        {
            BudgetSpaceId = request.BudgetSpaceId, Name = request.Name!.Trim(), AppliesTo = request.AppliesTo,
            ParentCategoryId = request.ParentCategoryId, SortOrder = request.SortOrder
        };
        database.Categories.Add(category);
        await database.SaveChangesAsync(token);
        return category;
    }

    public Task<List<FinancialTransaction>> GetTransactionsAsync(string ownerId, CancellationToken token)
    {
        return TransactionQuery(ownerId)
            .OrderByDescending(item => item.OccurredOn).ThenByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id).ToListAsync(token);
    }

    public async Task<TransactionPageResponse> GetTransactionPageAsync(string ownerId, TransactionPageQuery filter, CancellationToken token)
    {
        var query = TransactionQuery(ownerId);
        if (filter.From is not null) query = query.Where(item => item.OccurredOn >= filter.From.Value);
        if (filter.To is not null) query = query.Where(item => item.OccurredOn <= filter.To.Value);
        if (filter.Kinds.Length > 0) query = query.Where(item => filter.Kinds.Contains(item.Kind));
        if (filter.AccountIds.Length > 0) query = query.Where(item => filter.AccountIds.Contains(item.AccountId));
        if (filter.CategoryIds.Length > 0) query = query.Where(item => item.CategoryId.HasValue && filter.CategoryIds.Contains(item.CategoryId.Value));

        var total = await query.CountAsync(token);
        var items = await query.OrderByDescending(item => item.OccurredOn).ThenByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id).Skip(filter.Offset).Take(filter.Limit)
            .Select(item => new TransactionResponse(item.Id, item.Kind, item.Amount, item.OccurredOn, item.Description,
                item.AccountId, item.Account!.Name, item.CategoryId, item.Category == null ? null : item.Category.Name))
            .ToListAsync(token);
        return new TransactionPageResponse(items, total, filter.Offset, filter.Limit);
    }

    public async Task<IReadOnlyList<ReportCategoryResponse>> GetReportCategoriesAsync(string ownerId, DateOnly from, DateOnly to, CancellationToken token)
    {
        return await TransactionQuery(ownerId)
            .Where(item => item.OccurredOn >= from && item.OccurredOn <= to &&
                (item.Kind == TransactionKind.Income || item.Kind == TransactionKind.Expense))
            .GroupBy(item => new { item.Kind, item.CategoryId, Category = item.Category == null ? null : item.Category.Name })
            .Select(group => new ReportCategoryResponse(group.Key.Kind, group.Key.CategoryId, group.Key.Category,
                group.Sum(item => item.Amount), group.Count()))
            .ToListAsync(token);
    }

    public Task<List<FinancialTransaction>> GetTransactionsAsync(
        string ownerId,
        DateOnly? from,
        DateOnly? to,
        int? take,
        CancellationToken token)
    {
        var query = TransactionQuery(ownerId);
        if (from is not null) query = query.Where(item => item.OccurredOn >= from.Value);
        if (to is not null) query = query.Where(item => item.OccurredOn <= to.Value);
        query = query.OrderByDescending(item => item.OccurredOn).ThenByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id);
        if (take is not null) query = query.Take(take.Value);
        return query.ToListAsync(token);
    }

    public async Task<FinancialTransaction?> CreateTransactionAsync(string ownerId, TransactionRequest request,
        CancellationToken token)
    {
        var account = await database.Accounts.Include(item => item.BudgetSpace)
            .SingleOrDefaultAsync(item => item.Id == request.AccountId && item.BudgetSpace!.OwnerId == ownerId, token);
        var category = await database.Categories.FindAsync([request.CategoryId], token);
        if (account is null || category is null || !account.IsActive || !category.IsActive ||
            category.BudgetSpaceId != account.BudgetSpaceId || category.AppliesTo != request.Kind) return null;
        var item = new FinancialTransaction
        {
            AccountId = account.Id, CategoryId = category.Id, Kind = request.Kind, Amount = request.Amount,
            OccurredOn = request.OccurredOn, Description = request.Description?.Trim()
        };
        database.Transactions.Add(item);
        await database.SaveChangesAsync(token);
        return item;
    }

    public async Task<Transfer?> CreateTransferAsync(string ownerId, TransferRequest request, CancellationToken token)
    {
        var accounts = await database.Accounts.Include(account => account.BudgetSpace).Where(account =>
            (account.Id == request.SourceAccountId || account.Id == request.DestinationAccountId) &&
            account.BudgetSpace!.OwnerId == ownerId && account.IsActive).ToListAsync(token);
        if (accounts.Count != 2) return null;
        var transfer = new Transfer
        {
            SourceAccountId = request.SourceAccountId, DestinationAccountId = request.DestinationAccountId,
            Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim()
        };
        database.Transfers.Add(transfer);
        database.Transactions.AddRange(
            new FinancialTransaction
            {
                AccountId = transfer.SourceAccountId, Transfer = transfer, Kind = TransactionKind.TransferDebit,
                Amount = transfer.Amount, OccurredOn = transfer.OccurredOn, Description = transfer.Description
            },
            new FinancialTransaction
            {
                AccountId = transfer.DestinationAccountId, Transfer = transfer, Kind = TransactionKind.TransferCredit,
                Amount = transfer.Amount, OccurredOn = transfer.OccurredOn, Description = transfer.Description
            });
        await database.SaveChangesAsync(token);
        return transfer;
    }

    private IQueryable<FinancialTransaction> TransactionQuery(string ownerId)
    {
        return database.Transactions.AsNoTracking()
            .Include(item => item.Account)
            .Include(item => item.Category)
            .Where(item => item.Account!.BudgetSpace!.OwnerId == ownerId);
    }
}
