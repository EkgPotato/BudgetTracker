using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Finance;

public sealed record TransactionRequest(
    Guid AccountId,
    Guid CategoryId,
    TransactionKind Kind,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description);