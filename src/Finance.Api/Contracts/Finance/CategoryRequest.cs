using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Finance;

public sealed record CategoryRequest(
    Guid BudgetSpaceId,
    string? Name,
    TransactionKind AppliesTo,
    Guid? ParentCategoryId,
    int SortOrder);