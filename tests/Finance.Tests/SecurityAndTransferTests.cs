using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Finance.Api.Contracts.Auth;
using Finance.Api.Contracts.Finance;
using Finance.Api.Managers;
using Finance.Api.Services;
using Finance.Domain.Entities;
using Finance.Domain.Enums;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Finance.Tests;

public sealed class SecurityAndTransferTests : IClassFixture<ApiContractTests.TestApiFactory>
{
    private readonly ApiContractTests.TestApiFactory factory;
    public SecurityAndTransferTests(ApiContractTests.TestApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Repeated_bad_passwords_lock_the_account()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<FinanceUser>>();
        var manager = scope.ServiceProvider.GetRequiredService<IAuthManager>();
        var email = $"locked-{Guid.NewGuid():N}@example.test";
        var user = new FinanceUser { UserName = email, Email = email, EmailConfirmed = true };
        Assert.True((await users.CreateAsync(user, "CorrectPass123!")).Succeeded);

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Null(await manager.LoginAsync(new LoginRequest(email, "WrongPass123!")));

        Assert.True(await users.IsLockedOutAsync(user));
        Assert.Null(await manager.LoginAsync(new LoginRequest(email, "CorrectPass123!")));
    }

    [Fact]
    public async Task Login_endpoint_limits_repeated_requests()
    {
        using var client = factory.CreateClient();
        System.Net.Http.HttpResponseMessage? response = null;
        for (var attempt = 0; attempt < 21; attempt++)
        {
            response?.Dispose();
            response = await client.PostAsJsonAsync("/api/auth/login", new { email = "missing@example.test", password = "WrongPass123!" });
        }
        using (response) Assert.Equal(HttpStatusCode.TooManyRequests, response!.StatusCode);
    }

    [Fact]
    public async Task Onboarding_works_with_the_in_memory_test_database()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<FinanceUser>>();
        var onboarding = scope.ServiceProvider.GetRequiredService<IOnboardingService>();
        var email = $"onboarding-{Guid.NewGuid():N}@example.test";
        var user = new FinanceUser { UserName = email, Email = email };
        Assert.True((await users.CreateAsync(user, "CorrectPass123!")).Succeeded);
        await onboarding.CompleteAsync(user, new CompleteOnboardingRequest("Name", "Budget", "Account", AccountType.Bank, 10m), CancellationToken.None);
        Assert.True(user.OnboardingCompleted);
        var database = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        Assert.Single(database.Accounts.Where(account => account.BudgetSpace!.OwnerId == user.Id));
    }

    [Fact]
    public async Task Transfer_entries_are_visible_only_to_their_owner()
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IFinanceDataService>();
        var owner = Guid.NewGuid().ToString();
        var outsider = Guid.NewGuid().ToString();
        var budget = new BudgetSpace { OwnerId = owner, Name = "Budget" };
        var source = new Account { BudgetSpace = budget, Name = "Source", Type = AccountType.Bank };
        var destination = new Account { BudgetSpace = budget, Name = "Destination", Type = AccountType.Bank };
        database.Accounts.AddRange(source, destination);
        await database.SaveChangesAsync();

        var request = new TransferRequest(source.Id, destination.Id, 12.34m, new DateOnly(2026, 1, 1), "Transfer");
        Assert.Null(await service.CreateTransferAsync(outsider, request, CancellationToken.None));
        Assert.NotNull(await service.CreateTransferAsync(owner, request, CancellationToken.None));

        var ownerRows = await service.GetTransactionsAsync(owner, CancellationToken.None);
        Assert.Equal(2, ownerRows.Count);
        Assert.Contains(ownerRows, row => row.Kind == TransactionKind.TransferDebit && row.Amount == 12.34m);
        Assert.Contains(ownerRows, row => row.Kind == TransactionKind.TransferCredit && row.Amount == 12.34m);
        Assert.Empty(await service.GetTransactionsAsync(outsider, CancellationToken.None));
    }
}
