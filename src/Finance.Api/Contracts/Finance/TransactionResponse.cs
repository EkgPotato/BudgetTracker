using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Finance;

public sealed record TransactionResponse(
    Guid Id,
    TransactionKind Kind,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description,
    Guid AccountId,
    string AccountName,
    Guid? CategoryId,
    string? CategoryName);