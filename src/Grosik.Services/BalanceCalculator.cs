using Finance.Api.Domain;

namespace Grosik.Services;

public static class BalanceCalculator
{
    public static decimal Apply(TransactionKind kind, decimal amount) => kind switch
    {
        TransactionKind.Expense or TransactionKind.TransferDebit => -amount,
        _ => amount
    };

    public static decimal Sum(IEnumerable<FinancialTransaction> transactions) =>
        transactions.Sum(item => Apply(item.Kind, item.Amount));
}
