namespace Finance.Api.Contracts.Finance;

public sealed record TransactionPageResponse(
    IReadOnlyList<TransactionResponse> Items,
    int Total,
    int Offset,
    int Limit);
