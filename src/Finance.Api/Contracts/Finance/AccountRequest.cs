using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Finance;

public sealed record AccountRequest(
    Guid BudgetSpaceId,
    string? Name,
    AccountType Type,
    decimal? OpeningBalance,
    DateOnly? OpeningBalanceDate);