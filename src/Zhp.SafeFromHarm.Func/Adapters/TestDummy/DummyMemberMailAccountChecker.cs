using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyMemberMailAccountChecker : IMemberMailAccountChecker
{
    public Task<bool> HasEmailAccount(string membershipId, CancellationToken cancellationToken)
        => Task.FromResult(membershipId.Length % 2 == 0);
}
