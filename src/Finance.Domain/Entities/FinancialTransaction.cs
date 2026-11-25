using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class FinancialTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? TransferId { get; set; }
    public Transfer? Transfer { get; set; }
    public TransactionKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}