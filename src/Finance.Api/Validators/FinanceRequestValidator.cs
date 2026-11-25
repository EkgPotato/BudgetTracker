using Finance.Api.Contracts.Finance;
using Finance.Domain.Enums;
using Finance.Localization;

namespace Finance.Api.Validators;

public sealed class FinanceRequestValidator
{
    public Dictionary<string, string[]> Validate(BudgetSpaceRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Name)
            ? new Dictionary<string, string[]> { ["name"] = [ValidationMessages.Get("NameRequired")] }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(AccountRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Name) || request.OpeningBalance < 0
            ? new Dictionary<string, string[]> { ["account"] = [ValidationMessages.Get("AccountInvalid")] }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(CategoryRequest request)
    {
        return string.IsNullOrWhiteSpace(request.Name) ||
               request.AppliesTo is not (TransactionKind.Income or TransactionKind.Expense)
            ? new Dictionary<string, string[]> { ["category"] = [ValidationMessages.Get("CategoryInvalid")] }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(TransactionRequest request)
    {
        return request.Amount <= 0 || request.Kind is not (TransactionKind.Income or TransactionKind.Expense)
            ? new Dictionary<string, string[]>
            {
                ["transaction"] = [ValidationMessages.Get("TransactionInvalid")]
            }
            : new Dictionary<string, string[]>();
    }

    public Dictionary<string, string[]> Validate(TransferRequest request)
    {
        return request.Amount <= 0 || request.SourceAccountId == request.DestinationAccountId
            ? new Dictionary<string, string[]> { ["transfer"] = [ValidationMessages.Get("TransferInvalid")] }
            : new Dictionary<string, string[]>();
    }
}
