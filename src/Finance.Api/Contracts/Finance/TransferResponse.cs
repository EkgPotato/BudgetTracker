namespace Finance.Api.Contracts.Finance;

public sealed record TransferResponse(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description);