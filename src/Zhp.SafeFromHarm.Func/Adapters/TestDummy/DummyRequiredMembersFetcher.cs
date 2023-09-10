using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyRequiredMembersFetcher : IRequiredMembersFetcher
{
    public IAsyncEnumerable<ZhpMember> GetMembersRequiredToCertify()
        => new ZhpMember[]
        {
            new("Jan", "Kowalski", "AA01"),
            new("Jan", "Kowalski", "AA02"),
            new("Anna", "Nowak", "AA03"),
        }.ToAsyncEnumerable();
}
