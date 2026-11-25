using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Identity;
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
            throw new InvalidOperationException(
                "Configure Testing:DemoPassword before starting the testing environment.");

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
            throw new InvalidOperationException($"Nie udało się utworzyć użytkownika demonstracyjnego: {string.Join(' ', userResult.Errors.Select(error => error.Description))}");

        var space = new BudgetSpace { Name = "Budżet domowy", OwnerId = user.Id };
        var personalAccount = new Account { BudgetSpace = space, Name = "Konto osobiste", Type = AccountType.Bank };
        var savingsAccount = new Account { BudgetSpace = space, Name = "Oszczędności", Type = AccountType.Savings };
        var cashAccount = new Account { BudgetSpace = space, Name = "Portfel", Type = AccountType.Cash };

        var salary = new Category { BudgetSpace = space, Name = "Wynagrodzenie", AppliesTo = TransactionKind.Income, SortOrder = 10 };
        var home = new Category { BudgetSpace = space, Name = "Dom", AppliesTo = TransactionKind.Expense, SortOrder = 20 };
        var groceries = new Category { BudgetSpace = space, ParentCategory = home, Name = "Zakupy spożywcze", AppliesTo = TransactionKind.Expense, SortOrder = 21 };
        var bills = new Category { BudgetSpace = space, ParentCategory = home, Name = "Rachunki", AppliesTo = TransactionKind.Expense, SortOrder = 22 };
        var transport = new Category { BudgetSpace = space, Name = "Transport", AppliesTo = TransactionKind.Expense, SortOrder = 40 };
        var leisure = new Category { BudgetSpace = space, Name = "Rozrywka", AppliesTo = TransactionKind.Expense, SortOrder = 50 };
        var today = DateOnly.FromDateTime(DateTime.Today);

        database.AddRange(space, personalAccount, savingsAccount, cashAccount, salary, groceries, home, bills, transport, leisure);
        database.Transactions.AddRange(
            new FinancialTransaction { Account = personalAccount, Kind = TransactionKind.OpeningBalance, Amount = 3_200m, OccurredOn = today.AddDays(-20), Description = "Saldo otwarcia" },
            new FinancialTransaction { Account = personalAccount, Category = salary, Kind = TransactionKind.Income, Amount = 8_700m, OccurredOn = today.AddDays(-10), Description = "Wynagrodzenie" },
            new FinancialTransaction { Account = personalAccount, Category = bills, Kind = TransactionKind.Expense, Amount = 1_420m, OccurredOn = today.AddDays(-8), Description = "Czynsz i media" },
            new FinancialTransaction { Account = personalAccount, Category = groceries, Kind = TransactionKind.Expense, Amount = 286.40m, OccurredOn = today.AddDays(-5), Description = "Zakupy tygodniowe" },
            new FinancialTransaction { Account = personalAccount, Category = transport, Kind = TransactionKind.Expense, Amount = 154.80m, OccurredOn = today.AddDays(-3), Description = "Paliwo" },
            new FinancialTransaction { Account = personalAccount, Category = leisure, Kind = TransactionKind.Expense, Amount = 89.90m, OccurredOn = today.AddDays(-1), Description = "Kino" },
            new FinancialTransaction { Account = savingsAccount, Kind = TransactionKind.OpeningBalance, Amount = 12_500m, OccurredOn = today.AddDays(-20), Description = "Saldo oszczędności" },
            new FinancialTransaction { Account = cashAccount, Kind = TransactionKind.OpeningBalance, Amount = 250m, OccurredOn = today.AddDays(-20), Description = "Gotówka" });
        await database.SaveChangesAsync(cancellationToken);
    }
}
