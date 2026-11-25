using Microsoft.AspNetCore.Identity;

namespace Finance.Infrastructure.Identity;

public sealed class FinanceUser : IdentityUser
{
    public bool IsAdmin { get; set; }
    public string? DisplayName { get; set; }
    public bool OnboardingCompleted { get; set; }
}