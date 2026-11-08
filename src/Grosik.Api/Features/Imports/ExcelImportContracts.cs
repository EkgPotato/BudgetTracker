using Finance.Api.Domain;

namespace Finance.Api.Features.Imports;

public sealed record ExcelImportPreview(
    string SuggestedBudgetSpace,
    IReadOnlyList<ImportedAccountPreview> Accounts,
    IReadOnlyList<ImportedCategoryPreview> Categories,
    IReadOnlyList<string> Warnings);

public sealed record ImportedAccountPreview(string Name, decimal? OpeningBalance, AccountType SuggestedType);

public sealed record ImportedCategoryPreview(string Name, string? ParentName, TransactionKind AppliesTo);

public sealed record ExcelImportCommitRequest(
    string BudgetSpaceName,
    IReadOnlyList<ImportedAccountPreview> Accounts,
    IReadOnlyList<ImportedCategoryPreview> Categories,
    DateOnly OpeningBalanceDate);

public sealed record ExcelImportCommitResult(Guid BudgetSpaceId, int AccountsCreated, int CategoriesCreated, int OpeningBalancesCreated);
