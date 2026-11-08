using Finance.Api.Domain;

namespace Finance.Api.Contracts;

public sealed record BudgetSpaceRequest(string Name);
public sealed record AccountRequest(Guid BudgetSpaceId, string Name, AccountType Type, decimal? OpeningBalance, DateOnly? OpeningBalanceDate);
public sealed record CategoryRequest(Guid BudgetSpaceId, string Name, TransactionKind AppliesTo, Guid? ParentCategoryId, int SortOrder);
public sealed record TransactionRequest(Guid AccountId, Guid CategoryId, TransactionKind Kind, decimal Amount, DateOnly OccurredOn, string? Description);
public sealed record TransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, DateOnly OccurredOn, string? Description);
