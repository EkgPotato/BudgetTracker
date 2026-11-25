using Finance.Infrastructure.Identity;

namespace Finance.Api.Managers;

public interface IJwtTokenManager
{
    string Create(FinanceUser user);
}
