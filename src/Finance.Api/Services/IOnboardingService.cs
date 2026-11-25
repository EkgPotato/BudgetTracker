using Finance.Api.Contracts.Auth;
using Finance.Infrastructure.Identity;

namespace Finance.Api.Services;

public interface IOnboardingService
{
    Task CompleteAsync(FinanceUser user, CompleteOnboardingRequest request, CancellationToken cancellationToken);
}