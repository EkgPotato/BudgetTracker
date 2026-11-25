using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Finance;

public sealed record ReportCategoryResponse(
    TransactionKind Kind,
    Guid? CategoryId,
    string? Category,
    decimal Total,
    int Count);
