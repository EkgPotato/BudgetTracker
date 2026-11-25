using Finance.Domain.Entities;
using Finance.Domain.Enums;

namespace Finance.Domain.Services;

public static class BalanceCalculator
{
    public static decimal Apply(TransactionKind kind, decimal amount)
    {
        return kind switch
        {
            TransactionKind.Expense or TransactionKind.TransferDebit => -amount,
            _ => amount
        };
    }

    public static decimal Sum(IEnumerable<FinancialTransaction> transactions)
    {
        return transactions.Sum(transaction => Apply(transaction.Kind, transaction.Amount));
    }
}