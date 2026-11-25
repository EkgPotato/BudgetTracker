using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Finance;

public sealed class TransactionPageQuery
{
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public TransactionKind[] Kinds { get; init; } = [];
    public Guid[] AccountIds { get; init; } = [];
    public Guid[] CategoryIds { get; init; } = [];
    public int Offset { get; init; }
    public int Limit { get; init; } = 25;
}
