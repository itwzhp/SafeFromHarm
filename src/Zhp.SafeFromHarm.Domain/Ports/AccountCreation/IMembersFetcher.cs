using Zhp.SafeFromHarm.Domain.Model.AccountCreation;

namespace Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

public interface IMembersFetcher
{
    Task<Member?> GetMember(string membershipId, CancellationToken cancellationToken);
}
