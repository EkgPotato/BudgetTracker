using Finance.Api.Contracts.Finance;
using Finance.Domain.Enums;
using Finance.Localization;

namespace Finance.Api.Validators;

public sealed class FinanceRequestValidator
{
    public Dictionary<string, string[]> Validate(BudgetSpaceRequest request)
    {
        return !InputLimits.IsValidName(request.Name)
            ? new Dictionary<string, string[]> { ["name"] = [ValidationMessages.Get("NameRequired")] }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(AccountRequest request)
    {
        return !InputLimits.IsValidName(request.Name) || (request.OpeningBalance.HasValue && !InputLimits.IsValidAmount(request.OpeningBalance.Value)) || !Enum.IsDefined(request.Type)
            ? new Dictionary<string, string[]> { ["account"] = [ValidationMessages.Get("AccountInvalid")] }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(CategoryRequest request)
    {
        return !InputLimits.IsValidName(request.Name) ||
               request.AppliesTo is not (TransactionKind.Income or TransactionKind.Expense)
            ? new Dictionary<string, string[]> { ["category"] = [ValidationMessages.Get("CategoryInvalid")] }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(TransactionRequest request)
    {
        return request.Amount <= 0 || !InputLimits.IsValidAmount(request.Amount) || request.Description?.Length > InputLimits.DescriptionLength || request.OccurredOn == default || request.Kind is not (TransactionKind.Income or TransactionKind.Expense)
            ? new Dictionary<string, string[]>
            {
                ["transaction"] = [ValidationMessages.Get("TransactionInvalid")]
            }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(TransferRequest request)
    {
        return request.Amount <= 0 || !InputLimits.IsValidAmount(request.Amount) || request.Description?.Length > InputLimits.DescriptionLength || request.OccurredOn == default || request.SourceAccountId == request.DestinationAccountId
            ? new Dictionary<string, string[]> { ["transfer"] = [ValidationMessages.Get("TransferInvalid")] }
            : new Dictionary<string, string[]>();
    }
}
