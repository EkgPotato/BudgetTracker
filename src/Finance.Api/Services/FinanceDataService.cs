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
            .OrderByDescending(item => item.OccurredOn).ToListAsync(token);
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
        query = query.OrderByDescending(item => item.OccurredOn);
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
            .Where(item => item.TransferId == null && item.Account!.BudgetSpace!.OwnerId == ownerId);
    }
}
