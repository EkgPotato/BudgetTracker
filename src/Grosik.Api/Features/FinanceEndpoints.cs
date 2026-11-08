using Finance.Api.Contracts;
using Finance.Api.Data;
using Finance.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Api.Features;

public static class FinanceEndpoints
{
    public static RouteGroupBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        var finance = app.MapGroup("/api").RequireAuthorization();
        MapBudgetSpaces(finance);
        MapAccounts(finance);
        MapCategories(finance);
        MapTransactions(finance);
        MapTransfers(finance);
        MapReports(finance);
        MapDashboard(finance);
        return finance;
    }

    private static void MapBudgetSpaces(RouteGroupBuilder api)
    {
        api.MapGet("/budget-spaces", async (FinanceDbContext db) => await db.BudgetSpaces.AsNoTracking().OrderBy(x => x.Name).ToListAsync());
        api.MapPost("/budget-spaces", async (BudgetSpaceRequest request, FinanceDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nazwa jest wymagana."] });
            var space = new BudgetSpace { Name = request.Name.Trim() };
            db.BudgetSpaces.Add(space); await db.SaveChangesAsync();
            return Results.Created($"/api/budget-spaces/{space.Id}", space);
        });
        api.MapPut("/budget-spaces/{id:guid}", async (Guid id, BudgetSpaceRequest request, FinanceDbContext db) =>
        {
            var entity = await db.BudgetSpaces.FindAsync(id); if (entity is null) return Results.NotFound();
            entity.Name = request.Name.Trim(); await db.SaveChangesAsync(); return Results.Ok(entity);
        });
    }

    private static void MapAccounts(RouteGroupBuilder api)
    {
        api.MapGet("/accounts", async (Guid? budgetSpaceId, FinanceDbContext db) =>
        {
            var query = db.Accounts.AsNoTracking().Include(x => x.BudgetSpace).AsQueryable();
            if (budgetSpaceId.HasValue) query = query.Where(x => x.BudgetSpaceId == budgetSpaceId);
            return await query.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Type, x.Currency, x.IsActive, x.BudgetSpaceId, BudgetSpaceName = x.BudgetSpace!.Name, Balance = x.Transactions.Sum(t => t.Kind == TransactionKind.Expense || t.Kind == TransactionKind.TransferDebit ? -t.Amount : t.Amount) }).ToListAsync();
        });
        api.MapPost("/accounts", async (AccountRequest request, FinanceDbContext db) =>
        {
            if (request.OpeningBalance is < 0) return Results.ValidationProblem(new Dictionary<string, string[]> { ["openingBalance"] = ["Saldo otwarcia nie może być ujemne; dla zobowiązania użyj dodatniej kwoty."] });
            if (!await db.BudgetSpaces.AnyAsync(x => x.Id == request.BudgetSpaceId)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["budgetSpaceId"] = ["Portfel nie istnieje."] });
            var account = new Account { BudgetSpaceId = request.BudgetSpaceId, Name = request.Name.Trim(), Type = request.Type };
            db.Accounts.Add(account);
            if (request.OpeningBalance is > 0) db.Transactions.Add(new FinancialTransaction { Account = account, Amount = request.OpeningBalance.Value, Kind = TransactionKind.OpeningBalance, OccurredOn = request.OpeningBalanceDate ?? DateOnly.FromDateTime(DateTime.Today), Description = "Saldo otwarcia" });
            await db.SaveChangesAsync(); return Results.Created($"/api/accounts/{account.Id}", account);
        });
        api.MapPatch("/accounts/{id:guid}/active", async (Guid id, bool isActive, FinanceDbContext db) =>
        {
            var account = await db.Accounts.FindAsync(id); if (account is null) return Results.NotFound(); account.IsActive = isActive; await db.SaveChangesAsync(); return Results.NoContent();
        });
    }

    private static void MapCategories(RouteGroupBuilder api)
    {
        api.MapGet("/categories", async (Guid? budgetSpaceId, FinanceDbContext db) =>
        {
            var query = db.Categories.AsNoTracking().AsQueryable(); if (budgetSpaceId.HasValue) query = query.Where(x => x.BudgetSpaceId == budgetSpaceId);
            return await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync();
        });
        api.MapPost("/categories", async (CategoryRequest request, FinanceDbContext db) =>
        {
            if (request.AppliesTo is not (TransactionKind.Income or TransactionKind.Expense)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["appliesTo"] = ["Kategoria musi dotyczyć przychodu albo wydatku."] });
            if (request.ParentCategoryId.HasValue && !await db.Categories.AnyAsync(x => x.Id == request.ParentCategoryId && x.BudgetSpaceId == request.BudgetSpaceId && x.ParentCategoryId == null)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentCategoryId"] = ["Nieprawidłowa kategoria nadrzędna."] });
            var category = new Category { BudgetSpaceId = request.BudgetSpaceId, Name = request.Name.Trim(), AppliesTo = request.AppliesTo, ParentCategoryId = request.ParentCategoryId, SortOrder = request.SortOrder };
            db.Categories.Add(category); await db.SaveChangesAsync(); return Results.Created($"/api/categories/{category.Id}", category);
        });
        api.MapPatch("/categories/{id:guid}/active", async (Guid id, bool isActive, FinanceDbContext db) => { var category = await db.Categories.FindAsync(id); if (category is null) return Results.NotFound(); category.IsActive = isActive; await db.SaveChangesAsync(); return Results.NoContent(); });
    }

    private static void MapTransactions(RouteGroupBuilder api)
    {
        api.MapGet("/transactions", async (Guid? accountId, Guid? budgetSpaceId, DateOnly? from, DateOnly? to, FinanceDbContext db) =>
        {
            var query = db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => x.TransferId == null).AsQueryable();
            if (accountId.HasValue) query = query.Where(x => x.AccountId == accountId);
            if (budgetSpaceId.HasValue) query = query.Where(x => x.Account!.BudgetSpaceId == budgetSpaceId);
            if (from.HasValue) query = query.Where(x => x.OccurredOn >= from.Value); if (to.HasValue) query = query.Where(x => x.OccurredOn <= to.Value);
            return await query.OrderByDescending(x => x.OccurredOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new { x.Id, x.Kind, x.Amount, x.OccurredOn, x.Description, x.AccountId, AccountName = x.Account!.Name, x.CategoryId, CategoryName = x.Category == null ? null : x.Category.Name }).ToListAsync();
        });
        api.MapPost("/transactions", async (TransactionRequest request, FinanceDbContext db) =>
        {
            if (request.Amount <= 0 || request.Kind is not (TransactionKind.Income or TransactionKind.Expense)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["amount"] = ["Kwota musi być dodatnia, a typ przychodem albo wydatkiem."] });
            var account = await db.Accounts.FindAsync(request.AccountId); var category = await db.Categories.FindAsync(request.CategoryId);
            if (account is null || !account.IsActive || category is null || !category.IsActive || category.BudgetSpaceId != account.BudgetSpaceId || category.AppliesTo != request.Kind) return Results.ValidationProblem(new Dictionary<string, string[]> { ["transaction"] = ["Konto i kategoria muszą być aktywne i należeć do tego samego portfela."] });
            var item = new FinancialTransaction { AccountId = request.AccountId, CategoryId = request.CategoryId, Kind = request.Kind, Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim() };
            db.Transactions.Add(item); await db.SaveChangesAsync(); return Results.Created($"/api/transactions/{item.Id}", item);
        });
    }

    private static void MapTransfers(RouteGroupBuilder api)
    {
        api.MapPost("/transfers", async (TransferRequest request, FinanceDbContext db) =>
        {
            if (request.Amount <= 0 || request.SourceAccountId == request.DestinationAccountId) return Results.ValidationProblem(new Dictionary<string, string[]> { ["transfer"] = ["Wybierz różne konta i dodatnią kwotę."] });
            var accounts = await db.Accounts.Where(x => x.Id == request.SourceAccountId || x.Id == request.DestinationAccountId).ToListAsync();
            if (accounts.Count != 2 || accounts.Any(x => !x.IsActive) || accounts.Any(x => x.Currency != "PLN")) return Results.ValidationProblem(new Dictionary<string, string[]> { ["transfer"] = ["Oba aktywne konta PLN muszą istnieć."] });
            var transfer = new Transfer { SourceAccountId = request.SourceAccountId, DestinationAccountId = request.DestinationAccountId, Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim() };
            db.Transfers.Add(transfer);
            db.Transactions.AddRange(new FinancialTransaction { AccountId = request.SourceAccountId, TransferId = transfer.Id, Kind = TransactionKind.TransferDebit, Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim() }, new FinancialTransaction { AccountId = request.DestinationAccountId, TransferId = transfer.Id, Kind = TransactionKind.TransferCredit, Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim() });
            await db.SaveChangesAsync(); return Results.Created($"/api/transfers/{transfer.Id}", transfer);
        });
    }

    private static void MapReports(RouteGroupBuilder api) => api.MapGet("/reports/monthly", async (int year, int month, Guid? budgetSpaceId, FinanceDbContext db) =>
    {
        if (month is < 1 or > 12) return Results.BadRequest("Nieprawidłowy miesiąc.");
        var from = new DateOnly(year, month, 1); var to = from.AddMonths(1);
        var query = db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => x.OccurredOn >= from && x.OccurredOn < to && (x.Kind == TransactionKind.Income || x.Kind == TransactionKind.Expense));
        if (budgetSpaceId.HasValue) query = query.Where(x => x.Account!.BudgetSpaceId == budgetSpaceId);
        var rows = await query.GroupBy(x => new { x.Kind, x.CategoryId, Category = x.Category!.Name }).Select(g => new { g.Key.Kind, g.Key.CategoryId, g.Key.Category, Total = g.Sum(x => x.Amount), Count = g.Count() }).OrderBy(x => x.Kind).ThenByDescending(x => x.Total).ToListAsync();
        return Results.Ok(new { Year = year, Month = month, Income = rows.Where(x => x.Kind == TransactionKind.Income).Sum(x => x.Total), Expenses = rows.Where(x => x.Kind == TransactionKind.Expense).Sum(x => x.Total), Categories = rows });
    });

    private static void MapDashboard(RouteGroupBuilder api) => api.MapGet("/dashboard", async (FinanceDbContext db) =>
    {
        var today = DateOnly.FromDateTime(DateTime.Today); var from = new DateOnly(today.Year, today.Month, 1); var to = from.AddMonths(1);
        var accounts = await db.Accounts.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Type, x.Currency, x.IsActive, x.BudgetSpaceId, BudgetSpaceName = x.BudgetSpace!.Name, Balance = x.Transactions.Sum(t => t.Kind == TransactionKind.Expense || t.Kind == TransactionKind.TransferDebit ? -t.Amount : t.Amount) }).ToListAsync();
        var current = db.Transactions.AsNoTracking().Where(x => x.OccurredOn >= from && x.OccurredOn < to);
        var recent = await db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => x.TransferId == null).OrderByDescending(x => x.OccurredOn).Take(8)
            .Select(x => new { x.Id, x.Kind, x.Amount, x.OccurredOn, x.Description, x.AccountId, AccountName = x.Account!.Name, x.CategoryId, CategoryName = x.Category == null ? null : x.Category.Name }).ToListAsync();
        return Results.Ok(new { TotalBalance = accounts.Sum(x => x.Balance), IncomeThisMonth = await current.Where(x => x.Kind == TransactionKind.Income).SumAsync(x => (decimal?)x.Amount) ?? 0, ExpenseThisMonth = await current.Where(x => x.Kind == TransactionKind.Expense).SumAsync(x => (decimal?)x.Amount) ?? 0, Accounts = accounts, RecentTransactions = recent });
    });
}
