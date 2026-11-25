using Finance.Domain.Enums;

namespace Finance.Api.Contracts.Auth;

public sealed record CompleteOnboardingRequest(
    string? DisplayName,
    string? BudgetSpaceName,
    string? AccountName,
    AccountType AccountType,
    decimal OpeningBalance);