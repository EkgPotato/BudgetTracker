namespace Finance.Domain.Entities;

public sealed class BudgetSpace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string OwnerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Account> Accounts { get; } = new List<Account>();
    public ICollection<Category> Categories { get; } = new List<Category>();
}