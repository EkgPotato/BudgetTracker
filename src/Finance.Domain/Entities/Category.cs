using Finance.Domain.Enums;

namespace Finance.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BudgetSpaceId { get; set; }
    public BudgetSpace? BudgetSpace { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> ChildCategories { get; } = new List<Category>();
    public string Name { get; set; } = string.Empty;
    public TransactionKind AppliesTo { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FinancialTransaction> Transactions { get; } = new List<FinancialTransaction>();
}