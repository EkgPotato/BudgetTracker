using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Finance;

public sealed record CategoryResponse(
    Guid Id,
    Guid BudgetSpaceId,
    Guid? ParentCategoryId,
    string Name,
    TransactionKind AppliesTo,
    int SortOrder,
    bool IsActive);