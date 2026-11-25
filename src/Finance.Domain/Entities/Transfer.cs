namespace Finance.Domain.Entities;

public sealed class Transfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceAccountId { get; set; }
    public Account? SourceAccount { get; set; }
    public Guid DestinationAccountId { get; set; }
    public Account? DestinationAccount { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string? Description { get; set; }
    public ICollection<FinancialTransaction> Transactions { get; } = new List<FinancialTransaction>();
}