using Finance.Api.Data;
using Finance.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Finance.Api.Features.Imports;

public static class ExcelImportEndpoints
{
    public static IEndpointRouteBuilder MapExcelImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/imports").WithTags("Imports").RequireAuthorization();
        group.MapPost("/preview", async (IFormFile file, ExcelBudgetParser parser) =>
        {
            if (file.Length == 0 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { message = "Wybierz niepusty plik .xlsx." });
            await using var stream = file.OpenReadStream();
            return Results.Ok(parser.Parse(stream, file.FileName));
        }).DisableAntiforgery();

        group.MapPost("/commit", async (ExcelImportCommitRequest request, FinanceDbContext db, CancellationToken ct) =>
        {
            var name = request.BudgetSpaceName.Trim();
            if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest(new { message = "Nazwa portfela jest wymagana." });
            var space = await db.BudgetSpaces.Include(x => x.Accounts).Include(x => x.Categories)
                .SingleOrDefaultAsync(x => x.Name == name, ct);
            if (space is null) { space = new BudgetSpace { Name = name }; db.BudgetSpaces.Add(space); }
            var accountsCreated = 0; var categoriesCreated = 0; var openingsCreated = 0;
            foreach (var input in request.Accounts.Where(x => !string.IsNullOrWhiteSpace(x.Name)).DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var account = space.Accounts.FirstOrDefault(x => x.Name.Equals(input.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                if (account is not null) continue;
                account = new Account { BudgetSpaceId = space.Id, Name = input.Name.Trim(), Type = input.SuggestedType };
                db.Accounts.Add(account); accountsCreated++;
                if (input.OpeningBalance is { } amount)
                {
                    // Wire the navigation as well as the generated Guid so EF keeps the
                    // relationship intact even when the account is inserted in this unit of work.
                    db.Transactions.Add(new FinancialTransaction { Account = account, AccountId = account.Id, Kind = TransactionKind.OpeningBalance, Amount = amount, OccurredOn = request.OpeningBalanceDate, Description = "Saldo otwarcia zaimportowane z Excela" });
                    openingsCreated++;
                }
            }
            await db.SaveChangesAsync(ct);
            foreach (var input in request.Categories.Where(x => !string.IsNullOrWhiteSpace(x.Name)).DistinctBy(x => (x.Name, x.AppliesTo)))
            {
                if (await db.Categories.AnyAsync(x => x.BudgetSpaceId == space.Id && x.Name == input.Name.Trim() && x.AppliesTo == input.AppliesTo, ct)) continue;
                db.Categories.Add(new Category { BudgetSpaceId = space.Id, Name = input.Name.Trim(), AppliesTo = input.AppliesTo }); categoriesCreated++;
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(new ExcelImportCommitResult(space.Id, accountsCreated, categoriesCreated, openingsCreated));
        });
        return app;
    }
}
