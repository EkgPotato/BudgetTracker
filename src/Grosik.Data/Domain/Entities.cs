using Finance.Api.Identity;

namespace Finance.Api.Domain;

public sealed class BudgetSpace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string OwnerId { get; set; }
    public AppUser? Owner { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetSpaceId { get; set; }
    public BudgetSpace? BudgetSpace { get; set; }
    public required string Name { get; set; }
    public AccountType Type { get; set; }
    public string Currency { get; set; } = "PLN";
    public bool IsActive { get; set; } = true;
    public ICollection<FinancialTransaction> Transactions { get; set; } = new List<FinancialTransaction>();
}

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetSpaceId { get; set; }
    public BudgetSpace? BudgetSpace { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public required string Name { get; set; }
    public TransactionKind AppliesTo { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class FinancialTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Account? Account { get; set; }
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public Guid? TransferId { get; set; }
    public required TransactionKind Kind { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Transfer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceAccountId { get; set; }
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly OccurredOn { get; set; }
    public string? Description { get; set; }
}
