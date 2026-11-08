using Microsoft.AspNetCore.Identity;
using Finance.Api.Domain;

namespace Finance.Api.Identity;

public sealed class AppUser : IdentityUser
{
    public bool IsAdmin { get; set; }
    public string? DisplayName { get; set; }
    public bool OnboardingCompleted { get; set; }
    public ICollection<BudgetSpace> BudgetSpaces { get; set; } = new List<BudgetSpace>();
}
