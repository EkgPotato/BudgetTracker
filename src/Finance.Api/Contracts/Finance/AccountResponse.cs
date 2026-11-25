using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Finance;

public sealed record AccountResponse(
    Guid Id,
    string Name,
    AccountType Type,
    string Currency,
    bool IsActive,
    Guid BudgetSpaceId,
    string BudgetSpaceName,
    decimal Balance);