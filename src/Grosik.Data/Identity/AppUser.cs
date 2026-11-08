using Microsoft.AspNetCore.Identity;

namespace Finance.Api.Identity;

public sealed class AppUser : IdentityUser
{
    public bool IsAdmin { get; set; }
}
