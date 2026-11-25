using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetSpaceId { get; set; }
    public BudgetSpace? BudgetSpace { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string Currency { get; set; } = "PLN";
    public bool IsActive { get; set; } = true;
    public ICollection<FinancialTransaction> Transactions { get; } = new List<FinancialTransaction>();
    public ICollection<Transfer> OutgoingTransfers { get; } = new List<Transfer>();
    public ICollection<Transfer> IncomingTransfers { get; } = new List<Transfer>();
}