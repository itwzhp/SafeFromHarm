using Zhp.SafeFromHarm.Domain.Model.AccountCreation;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyMembersFetcher : IMembersFetcher
{
    public Task<Member?> GetMember(string membershipId, CancellationToken cancellationToken)
        => Task.FromResult<Member?>(membershipId switch
        {
            "AB1234" => new("Jan", "Kowalski", membershipId),
            "AA111" => new("Anna", "Nowak", membershipId),
            _ => null,
        });
}
