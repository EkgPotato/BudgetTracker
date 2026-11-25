namespace Finance.Api.Contracts.Finance;

public sealed record TransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    DateOnly OccurredOn,
    string? Description);