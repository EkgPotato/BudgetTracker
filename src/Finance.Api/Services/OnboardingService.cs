using Finance.Api.Contracts.Auth;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace Finance.Api.Services;

public sealed class OnboardingService(FinanceDbContext database) : IOnboardingService
{
    public async Task CompleteAsync(FinanceUser user, CompleteOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        if (user.OnboardingCompleted) return;

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var existingAccount = await database.Accounts
            .AnyAsync(account => account.BudgetSpace!.OwnerId == user.Id, cancellationToken);

        if (!existingAccount)
        {
            var budgetSpace = new BudgetSpace
            {
                OwnerId = user.Id,
                Name = request.BudgetSpaceName!.Trim()
            };
            var account = new Account
            {
                BudgetSpace = budgetSpace,
                Name = request.AccountName!.Trim(),
                Type = request.AccountType
            };

            database.BudgetSpaces.Add(budgetSpace);
            database.Accounts.Add(account);
            if (request.OpeningBalance > 0)
                database.Transactions.Add(new FinancialTransaction
                {
                    Account = account,
                    Amount = request.OpeningBalance,
                    Kind = TransactionKind.OpeningBalance,
                    OccurredOn = DateOnly.FromDateTime(DateTime.Today),
                    Description = "Saldo otwarcia"
                });
        }

        user.DisplayName = request.DisplayName!.Trim();
        user.OnboardingCompleted = true;
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}