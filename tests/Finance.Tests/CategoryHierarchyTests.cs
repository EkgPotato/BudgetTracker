using System;
using System.Threading;
using Finance.Api.Contracts.Finance;
using Finance.Api.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;

namespace Finance.Tests;

public sealed class CategoryHierarchyTests
{
    [Fact]
    public async Task CreateCategory_RejectsParentFromAnotherBudgetSpace()
    {
        await using var database = CreateDatabase();
        var ownerId = "owner";
        var targetSpace = new BudgetSpace { OwnerId = ownerId, Name = "Domowy" };
        var otherSpace = new BudgetSpace { OwnerId = ownerId, Name = "Wspólny" };
        var parent = new Category
        {
            BudgetSpace = otherSpace,
            Name = "Dom",
            AppliesTo = TransactionKind.Expense
        };
        database.AddRange(targetSpace, otherSpace, parent);
        await database.SaveChangesAsync();

        var result = await new FinanceDataService(database).CreateCategoryAsync(ownerId,
            new CategoryRequest(targetSpace.Id, "Zakupy", TransactionKind.Expense, parent.Id, 0),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCategory_RejectsParentWithDifferentTransactionKind()
    {
        await using var database = CreateDatabase();
        var ownerId = "owner";
        var space = new BudgetSpace { OwnerId = ownerId, Name = "Domowy" };
        var incomeParent = new Category
        {
            BudgetSpace = space,
            Name = "Praca",
            AppliesTo = TransactionKind.Income
        };
        database.AddRange(space, incomeParent);
        await database.SaveChangesAsync();

        var result = await new FinanceDataService(database).CreateCategoryAsync(ownerId,
            new CategoryRequest(space.Id, "Zakupy", TransactionKind.Expense, incomeParent.Id, 0),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateCategory_AcceptsParentFromSameBudgetSpaceAndKind()
    {
        await using var database = CreateDatabase();
        var ownerId = "owner";
        var space = new BudgetSpace { OwnerId = ownerId, Name = "Domowy" };
        var parent = new Category
        {
            BudgetSpace = space,
            Name = "Dom",
            AppliesTo = TransactionKind.Expense
        };
        database.AddRange(space, parent);
        await database.SaveChangesAsync();

        var result = await new FinanceDataService(database).CreateCategoryAsync(ownerId,
            new CategoryRequest(space.Id, "Zakupy", TransactionKind.Expense, parent.Id, 0),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(parent.Id, result.ParentCategoryId);
    }

    private static FinanceDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FinanceDbContext(options);
    }
}
