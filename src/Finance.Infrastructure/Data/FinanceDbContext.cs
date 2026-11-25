using Finance.Domain.Entities;
using Finance.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Finance.Infrastructure.Data;

public sealed class FinanceDbContext(DbContextOptions<FinanceDbContext> options)
    : IdentityDbContext<FinanceUser>(options)
{
    public DbSet<BudgetSpace> BudgetSpaces => Set<BudgetSpace>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinancialTransaction> Transactions => Set<FinancialTransaction>();
    public DbSet<Transfer> Transfers => Set<Transfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BudgetSpace>(entity =>
        {
            entity.Property(space => space.Name)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(space => space.OwnerId)
                .HasMaxLength(450)
                .IsRequired();
            entity.HasIndex(space => new { space.OwnerId, space.Name })
                .IsUnique();
            entity.HasOne<FinanceUser>()
                .WithMany()
                .HasForeignKey(space => space.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.Property(account => account.Name)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(account => account.Currency)
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
            entity.HasIndex(account => new { account.BudgetSpaceId, account.Name })
                .IsUnique();
            entity.HasOne(account => account.BudgetSpace)
                .WithMany(space => space.Accounts)
                .HasForeignKey(account => account.BudgetSpaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name)
                .HasMaxLength(100)
                .IsRequired();
            entity.HasIndex(category => new { category.BudgetSpaceId, category.ParentCategoryId, category.Name })
                .IsUnique();
            entity.HasOne(category => category.BudgetSpace)
                .WithMany(space => space.Categories)
                .HasForeignKey(category => category.BudgetSpaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(category => category.ParentCategory)
                .WithMany(category => category.ChildCategories)
                .HasForeignKey(category => category.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.Property(transfer => transfer.Amount)
                .HasPrecision(19, 4);
            entity.Property(transfer => transfer.Description)
                .HasMaxLength(500);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_Transfers_Amount_Positive", "[Amount] > 0");
                table.HasCheckConstraint("CK_Transfers_Different_Accounts",
                    "[SourceAccountId] <> [DestinationAccountId]");
            });
            entity.HasIndex(transfer => transfer.SourceAccountId);
            entity.HasIndex(transfer => transfer.DestinationAccountId);
            entity.HasOne(transfer => transfer.SourceAccount)
                .WithMany(account => account.OutgoingTransfers)
                .HasForeignKey(transfer => transfer.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transfer => transfer.DestinationAccount)
                .WithMany(account => account.IncomingTransfers)
                .HasForeignKey(transfer => transfer.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FinancialTransaction>(entity =>
        {
            entity.Property(transaction => transaction.Amount)
                .HasPrecision(19, 4);
            entity.Property(transaction => transaction.Description)
                .HasMaxLength(500);
            entity.ToTable(table => table.HasCheckConstraint("CK_Transactions_Amount_Positive", "[Amount] > 0"));
            entity.HasIndex(transaction => new { transaction.AccountId, transaction.OccurredOn });
            entity.HasIndex(transaction => new { transaction.CategoryId, transaction.OccurredOn });
            entity.HasIndex(transaction => transaction.TransferId);
            entity.HasOne(transaction => transaction.Account)
                .WithMany(account => account.Transactions)
                .HasForeignKey(transaction => transaction.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transaction => transaction.Category)
                .WithMany(category => category.Transactions)
                .HasForeignKey(transaction => transaction.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transaction => transaction.Transfer)
                .WithMany(transfer => transfer.Transactions)
                .HasForeignKey(transaction => transaction.TransferId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
