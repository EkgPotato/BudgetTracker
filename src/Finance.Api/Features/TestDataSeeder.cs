using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Identity;
using Finance.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Finance.Api.Features;

public static class TestDataSeeder
{
    public const string DemoEmail = "demo@grosik.test";
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var demoPassword = configuration["Testing:DemoPassword"];
        if (string.IsNullOrWhiteSpace(demoPassword))
            throw new InvalidOperationException(ValidationMessages.Get("DemoPasswordMissing"));

        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        if (await database.Users.AnyAsync(cancellationToken)) return;

        var users = scope.ServiceProvider.GetRequiredService<UserManager<FinanceUser>>();
        var user = new FinanceUser
        {
            UserName = DemoEmail,
            Email = DemoEmail,
            EmailConfirmed = true,
            DisplayName = "Anna Kowalska",
            IsAdmin = true,
            OnboardingCompleted = true
        };
        var userResult = await users.CreateAsync(user, demoPassword);
        if (!userResult.Succeeded)
            throw new InvalidOperationException(ValidationMessages.Get("DemoUserCreationFailed"));

        var space = new BudgetSpace { Name = "Home budget", OwnerId = user.Id };
        var personalAccount = new Account { BudgetSpace = space, Name = "Personal account", Type = AccountType.Bank };
        var savingsAccount = new Account { BudgetSpace = space, Name = "Savings", Type = AccountType.Savings };
        var cashAccount = new Account { BudgetSpace = space, Name = "Wallet", Type = AccountType.Cash };

        var salary = new Category { BudgetSpace = space, Name = "Salary", AppliesTo = TransactionKind.Income, SortOrder = 10 };
        var home = new Category { BudgetSpace = space, Name = "Home", AppliesTo = TransactionKind.Expense, SortOrder = 20 };
        var groceries = new Category { BudgetSpace = space, ParentCategory = home, Name = "Groceries", AppliesTo = TransactionKind.Expense, SortOrder = 21 };
        var bills = new Category { BudgetSpace = space, ParentCategory = home, Name = "Bills", AppliesTo = TransactionKind.Expense, SortOrder = 22 };
        var transport = new Category { BudgetSpace = space, Name = "Transport", AppliesTo = TransactionKind.Expense, SortOrder = 40 };
        var leisure = new Category { BudgetSpace = space, Name = "Leisure", AppliesTo = TransactionKind.Expense, SortOrder = 50 };
        var today = DateOnly.FromDateTime(DateTime.Today);

        database.AddRange(space, personalAccount, savingsAccount, cashAccount, salary, groceries, home, bills, transport, leisure);
        database.Transactions.AddRange(
            new FinancialTransaction { Account = personalAccount, Kind = TransactionKind.OpeningBalance, Amount = 3_200m, OccurredOn = today.AddDays(-20), Description = "Opening balance" },
            new FinancialTransaction { Account = personalAccount, Category = salary, Kind = TransactionKind.Income, Amount = 8_700m, OccurredOn = today.AddDays(-10), Description = "Salary" },
            new FinancialTransaction { Account = personalAccount, Category = bills, Kind = TransactionKind.Expense, Amount = 1_420m, OccurredOn = today.AddDays(-8), Description = "Rent and utilities" },
            new FinancialTransaction { Account = personalAccount, Category = groceries, Kind = TransactionKind.Expense, Amount = 286.40m, OccurredOn = today.AddDays(-5), Description = "Weekly groceries" },
            new FinancialTransaction { Account = personalAccount, Category = transport, Kind = TransactionKind.Expense, Amount = 154.80m, OccurredOn = today.AddDays(-3), Description = "Fuel" },
            new FinancialTransaction { Account = personalAccount, Category = leisure, Kind = TransactionKind.Expense, Amount = 89.90m, OccurredOn = today.AddDays(-1), Description = "Cinema" },
            new FinancialTransaction { Account = savingsAccount, Kind = TransactionKind.OpeningBalance, Amount = 12_500m, OccurredOn = today.AddDays(-20), Description = "Opening savings balance" },
            new FinancialTransaction { Account = cashAccount, Kind = TransactionKind.OpeningBalance, Amount = 250m, OccurredOn = today.AddDays(-20), Description = "Cash" });
        await database.SaveChangesAsync(cancellationToken);
    }
}
