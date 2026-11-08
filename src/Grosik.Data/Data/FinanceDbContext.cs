using Finance.Api.Domain;
using Finance.Api.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Finance.Api.Data;

public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<BudgetSpace> BudgetSpaces => Set<BudgetSpace>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinancialTransaction> Transactions => Set<FinancialTransaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<BudgetSpace>().Property(x => x.Name).HasMaxLength(100);
        model.Entity<BudgetSpace>().Property(x => x.OwnerId).HasMaxLength(450);
        model.Entity<BudgetSpace>().HasOne(x => x.Owner).WithMany(x => x.BudgetSpaces).HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<BudgetSpace>().HasIndex(x => new { x.OwnerId, x.Name }).IsUnique();
        model.Entity<Account>().Property(x => x.Name).HasMaxLength(100);
        model.Entity<Account>().Property(x => x.Currency).HasMaxLength(3).IsFixedLength();
        model.Entity<Category>().Property(x => x.Name).HasMaxLength(100);
        model.Entity<FinancialTransaction>().Property(x => x.Amount).HasPrecision(19, 4);
        model.Entity<Transfer>().Property(x => x.Amount).HasPrecision(19, 4);
        model.Entity<Category>().HasOne(x => x.ParentCategory).WithMany().HasForeignKey(x => x.ParentCategoryId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<FinancialTransaction>().HasOne(x => x.Account).WithMany(x => x.Transactions).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<FinancialTransaction>().HasIndex(x => new { x.AccountId, x.OccurredOn });
        model.Entity<FinancialTransaction>().HasIndex(x => new { x.CategoryId, x.OccurredOn });
    }
}
