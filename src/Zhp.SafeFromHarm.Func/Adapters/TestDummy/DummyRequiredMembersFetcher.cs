using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyRequiredMembersFetcher : IRequiredMembersFetcher
{
    public IAsyncEnumerable<ZhpMember> GetMembersRequiredToCertify()
        => new ZhpMember[]
        {
            new("Jan", "Kowalski", "AA01", "hufiec@zhp.example.com"),
            new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com"),
            new("Tomasz", "Innyhufiec", "AB01", "drugihufiec@zhp.example.com"),
            new("Anna", "Nowak", "AA03", "hufiec@zhp.example.com"),
        }.ToAsyncEnumerable();
}
