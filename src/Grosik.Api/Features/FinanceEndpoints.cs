using Finance.Api.Contracts;
using Finance.Api.Data;
using Finance.Api.Domain;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Finance.Api.Features;

public static class FinanceEndpoints
{
    private static string? CurrentUserId(ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
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
        api.MapGet("/budget-spaces", async (HttpContext context, FinanceDbContext db) =>
        {
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            return Results.Ok(await db.BudgetSpaces.AsNoTracking().Where(x => x.OwnerId == ownerId).OrderBy(x => x.Name).ToListAsync());
        });
        api.MapPost("/budget-spaces", async (BudgetSpaceRequest request, HttpContext context, FinanceDbContext db) =>
        {
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.Name)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nazwa jest wymagana."] });
            var space = new BudgetSpace { Name = request.Name.Trim(), OwnerId = ownerId };
            db.BudgetSpaces.Add(space); await db.SaveChangesAsync();
            return Results.Created($"/api/budget-spaces/{space.Id}", space);
        });
        api.MapPut("/budget-spaces/{id:guid}", async (Guid id, BudgetSpaceRequest request, HttpContext context, FinanceDbContext db) =>
        {
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            var entity = await db.BudgetSpaces.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId); if (entity is null) return Results.NotFound();
            entity.Name = request.Name.Trim(); await db.SaveChangesAsync(); return Results.Ok(entity);
        });
    }

    private static void MapAccounts(RouteGroupBuilder api)
    {
        api.MapGet("/accounts", async (HttpContext context) =>
        {
            var db = context.RequestServices.GetRequiredService<FinanceDbContext>();
            Guid? budgetSpaceId = Guid.TryParse(context.Request.Query["budgetSpaceId"], out var parsedBudgetSpaceId) ? parsedBudgetSpaceId : null;
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            var query = db.Accounts.AsNoTracking().Include(x => x.BudgetSpace).Where(x => x.BudgetSpace!.OwnerId == ownerId).AsQueryable();
            if (budgetSpaceId.HasValue) query = query.Where(x => x.BudgetSpaceId == budgetSpaceId);
            return Results.Ok(await query.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Type, x.Currency, x.IsActive, x.BudgetSpaceId, BudgetSpaceName = x.BudgetSpace!.Name, Balance = x.Transactions.Sum(t => t.Kind == TransactionKind.Expense || t.Kind == TransactionKind.TransferDebit ? -t.Amount : t.Amount) }).ToListAsync());
        });
        api.MapPost("/accounts", async (AccountRequest request, HttpContext context, FinanceDbContext db) =>
        {
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            if (request.OpeningBalance is < 0) return Results.ValidationProblem(new Dictionary<string, string[]> { ["openingBalance"] = ["Saldo otwarcia nie może być ujemne; dla zobowiązania użyj dodatniej kwoty."] });
            if (!await db.BudgetSpaces.AnyAsync(x => x.Id == request.BudgetSpaceId && x.OwnerId == ownerId)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["budgetSpaceId"] = ["Portfel nie istnieje."] });
            var account = new Account { BudgetSpaceId = request.BudgetSpaceId, Name = request.Name.Trim(), Type = request.Type };
            db.Accounts.Add(account);
            if (request.OpeningBalance is > 0) db.Transactions.Add(new FinancialTransaction { Account = account, Amount = request.OpeningBalance.Value, Kind = TransactionKind.OpeningBalance, OccurredOn = request.OpeningBalanceDate ?? DateOnly.FromDateTime(DateTime.Today), Description = "Saldo otwarcia" });
            await db.SaveChangesAsync(); return Results.Created($"/api/accounts/{account.Id}", account);
        });
        api.MapPatch("/accounts/{id:guid}/active", async (Guid id, bool isActive, HttpContext context, FinanceDbContext db) =>
        {
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            var account = await db.Accounts.Include(x => x.BudgetSpace).SingleOrDefaultAsync(x => x.Id == id && x.BudgetSpace!.OwnerId == ownerId); if (account is null) return Results.NotFound(); account.IsActive = isActive; await db.SaveChangesAsync(); return Results.NoContent();
        });
    }

    private static void MapCategories(RouteGroupBuilder api)
    {
        api.MapGet("/categories", async (HttpContext context) =>
        {
            var db = context.RequestServices.GetRequiredService<FinanceDbContext>();
            Guid? budgetSpaceId = Guid.TryParse(context.Request.Query["budgetSpaceId"], out var parsedBudgetSpaceId) ? parsedBudgetSpaceId : null;
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            var query = db.Categories.AsNoTracking().Where(x => x.BudgetSpace!.OwnerId == ownerId).AsQueryable(); if (budgetSpaceId.HasValue) query = query.Where(x => x.BudgetSpaceId == budgetSpaceId);
            return Results.Ok(await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync());
        });
        api.MapPost("/categories", async (CategoryRequest request, HttpContext context, FinanceDbContext db) =>
        {
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            if (request.AppliesTo is not (TransactionKind.Income or TransactionKind.Expense)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["appliesTo"] = ["Kategoria musi dotyczyć przychodu albo wydatku."] });
            if (!await db.BudgetSpaces.AnyAsync(x => x.Id == request.BudgetSpaceId && x.OwnerId == ownerId)) return Results.NotFound();
            if (request.ParentCategoryId.HasValue && !await db.Categories.AnyAsync(x => x.Id == request.ParentCategoryId && x.BudgetSpaceId == request.BudgetSpaceId && x.ParentCategoryId == null)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["parentCategoryId"] = ["Nieprawidłowa kategoria nadrzędna."] });
            var category = new Category { BudgetSpaceId = request.BudgetSpaceId, Name = request.Name.Trim(), AppliesTo = request.AppliesTo, ParentCategoryId = request.ParentCategoryId, SortOrder = request.SortOrder };
            db.Categories.Add(category); await db.SaveChangesAsync(); return Results.Created($"/api/categories/{category.Id}", category);
        });
        api.MapPatch("/categories/{id:guid}/active", async (Guid id, bool isActive, HttpContext context, FinanceDbContext db) => { var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized(); var category = await db.Categories.Include(x => x.BudgetSpace).SingleOrDefaultAsync(x => x.Id == id && x.BudgetSpace!.OwnerId == ownerId); if (category is null) return Results.NotFound(); category.IsActive = isActive; await db.SaveChangesAsync(); return Results.NoContent(); });
    }

    private static void MapTransactions(RouteGroupBuilder api)
    {
        api.MapGet("/transactions", async (HttpContext context) =>
        {
            var db = context.RequestServices.GetRequiredService<FinanceDbContext>();
            Guid? accountId = Guid.TryParse(context.Request.Query["accountId"], out var parsedAccountId) ? parsedAccountId : null;
            Guid? budgetSpaceId = Guid.TryParse(context.Request.Query["budgetSpaceId"], out var parsedBudgetSpaceId) ? parsedBudgetSpaceId : null;
            DateOnly? from = DateOnly.TryParse(context.Request.Query["from"], out var parsedFrom) ? parsedFrom : null;
            DateOnly? to = DateOnly.TryParse(context.Request.Query["to"], out var parsedTo) ? parsedTo : null;
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            var query = db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => x.TransferId == null && x.Account!.BudgetSpace!.OwnerId == ownerId).AsQueryable();
            if (accountId.HasValue) query = query.Where(x => x.AccountId == accountId);
            if (budgetSpaceId.HasValue) query = query.Where(x => x.Account!.BudgetSpaceId == budgetSpaceId);
            if (from.HasValue) query = query.Where(x => x.OccurredOn >= from.Value); if (to.HasValue) query = query.Where(x => x.OccurredOn <= to.Value);
            return Results.Ok(await query.OrderByDescending(x => x.OccurredOn).ThenByDescending(x => x.CreatedAtUtc).Select(x => new { x.Id, x.Kind, x.Amount, x.OccurredOn, x.Description, x.AccountId, AccountName = x.Account!.Name, x.CategoryId, CategoryName = x.Category == null ? null : x.Category.Name }).ToListAsync());
        });
        api.MapPost("/transactions", async (TransactionRequest request, HttpContext context, FinanceDbContext db) =>
        {
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            if (request.Amount <= 0 || request.Kind is not (TransactionKind.Income or TransactionKind.Expense)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["amount"] = ["Kwota musi być dodatnia, a typ przychodem albo wydatkiem."] });
            var account = await db.Accounts.FindAsync(request.AccountId); var category = await db.Categories.FindAsync(request.CategoryId);
            if (account is null || !await db.BudgetSpaces.AnyAsync(x => x.Id == account.BudgetSpaceId && x.OwnerId == ownerId) || !account.IsActive || category is null || !category.IsActive || category.BudgetSpaceId != account.BudgetSpaceId || category.AppliesTo != request.Kind) return Results.ValidationProblem(new Dictionary<string, string[]> { ["transaction"] = ["Konto i kategoria muszą być aktywne i należeć do tego samego portfela."] });
            var item = new FinancialTransaction { AccountId = request.AccountId, CategoryId = request.CategoryId, Kind = request.Kind, Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim() };
            db.Transactions.Add(item); await db.SaveChangesAsync(); return Results.Created($"/api/transactions/{item.Id}", item);
        });
    }

    private static void MapTransfers(RouteGroupBuilder api)
    {
        api.MapPost("/transfers", async (TransferRequest request, HttpContext context, FinanceDbContext db) =>
        {
            var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
            if (request.Amount <= 0 || request.SourceAccountId == request.DestinationAccountId) return Results.ValidationProblem(new Dictionary<string, string[]> { ["transfer"] = ["Wybierz różne konta i dodatnią kwotę."] });
            var accounts = await db.Accounts.Include(x => x.BudgetSpace).Where(x => (x.Id == request.SourceAccountId || x.Id == request.DestinationAccountId) && x.BudgetSpace!.OwnerId == ownerId).ToListAsync();
            if (accounts.Count != 2 || accounts.Any(x => !x.IsActive) || accounts.Any(x => x.Currency != "PLN")) return Results.ValidationProblem(new Dictionary<string, string[]> { ["transfer"] = ["Oba aktywne konta PLN muszą istnieć."] });
            var transfer = new Transfer { SourceAccountId = request.SourceAccountId, DestinationAccountId = request.DestinationAccountId, Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim() };
            db.Transfers.Add(transfer);
            db.Transactions.AddRange(new FinancialTransaction { AccountId = request.SourceAccountId, TransferId = transfer.Id, Kind = TransactionKind.TransferDebit, Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim() }, new FinancialTransaction { AccountId = request.DestinationAccountId, TransferId = transfer.Id, Kind = TransactionKind.TransferCredit, Amount = request.Amount, OccurredOn = request.OccurredOn, Description = request.Description?.Trim() });
            await db.SaveChangesAsync(); return Results.Created($"/api/transfers/{transfer.Id}", transfer);
        });
    }

    private static void MapReports(RouteGroupBuilder api) => api.MapGet("/reports/monthly", async (int year, int month, Guid? budgetSpaceId, HttpContext context, FinanceDbContext db) =>
    {
        var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
        if (month is < 1 or > 12) return Results.BadRequest("Nieprawidłowy miesiąc.");
        var from = new DateOnly(year, month, 1); var to = from.AddMonths(1);
        var query = db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => x.Account!.BudgetSpace!.OwnerId == ownerId && x.OccurredOn >= from && x.OccurredOn < to && (x.Kind == TransactionKind.Income || x.Kind == TransactionKind.Expense));
        if (budgetSpaceId.HasValue) query = query.Where(x => x.Account!.BudgetSpaceId == budgetSpaceId);
        var rows = await query.GroupBy(x => new { x.Kind, x.CategoryId, Category = x.Category!.Name }).Select(g => new { g.Key.Kind, g.Key.CategoryId, g.Key.Category, Total = g.Sum(x => x.Amount), Count = g.Count() }).OrderBy(x => x.Kind).ThenByDescending(x => x.Total).ToListAsync();
        return Results.Ok(new { Year = year, Month = month, Income = rows.Where(x => x.Kind == TransactionKind.Income).Sum(x => x.Total), Expenses = rows.Where(x => x.Kind == TransactionKind.Expense).Sum(x => x.Total), Categories = rows });
    });

    private static void MapDashboard(RouteGroupBuilder api) => api.MapGet("/dashboard", async (HttpContext context, FinanceDbContext db) =>
    {
        var ownerId = CurrentUserId(context.User); if (ownerId is null) return Results.Unauthorized();
        var today = DateOnly.FromDateTime(DateTime.Today); var from = new DateOnly(today.Year, today.Month, 1); var to = from.AddMonths(1);
        var accounts = await db.Accounts.AsNoTracking().Where(x => x.IsActive && x.BudgetSpace!.OwnerId == ownerId).OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Type, x.Currency, x.IsActive, x.BudgetSpaceId, BudgetSpaceName = x.BudgetSpace!.Name, Balance = x.Transactions.Sum(t => t.Kind == TransactionKind.Expense || t.Kind == TransactionKind.TransferDebit ? -t.Amount : t.Amount) }).ToListAsync();
        var current = db.Transactions.AsNoTracking().Where(x => x.Account!.BudgetSpace!.OwnerId == ownerId && x.OccurredOn >= from && x.OccurredOn < to);
        var recent = await db.Transactions.AsNoTracking().Include(x => x.Account).Include(x => x.Category).Where(x => x.TransferId == null && x.Account!.BudgetSpace!.OwnerId == ownerId).OrderByDescending(x => x.OccurredOn).Take(8)
            .Select(x => new { x.Id, x.Kind, x.Amount, x.OccurredOn, x.Description, x.AccountId, AccountName = x.Account!.Name, x.CategoryId, CategoryName = x.Category == null ? null : x.Category.Name }).ToListAsync();
        return Results.Ok(new { TotalBalance = accounts.Sum(x => x.Balance), IncomeThisMonth = await current.Where(x => x.Kind == TransactionKind.Income).SumAsync(x => (decimal?)x.Amount) ?? 0, ExpenseThisMonth = await current.Where(x => x.Kind == TransactionKind.Expense).SumAsync(x => (decimal?)x.Amount) ?? 0, Accounts = accounts, RecentTransactions = recent });
    });
}
