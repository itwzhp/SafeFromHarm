using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyRequiredMembersFetcher : IRequiredMembersFetcher
{
    public IAsyncEnumerable<MemberToCertify> GetMembersRequiredToCertify()
        => new MemberToCertify[]
        {
            new("Jan", "Kowalski", "AA01", new(10, "Hufiec", "hufiec@zhp.example.com")),
            new("Jan", "Kowalski", "AA02", new(10, "Hufiec", "hufiec@zhp.example.com")),
            new("Tomasz", "Innyhufiec", "AB01", new(11, "Drugi Hufiec", "drugihufiec@zhp.example.com")),
            new("Anna", "Nowak", "AA03", new(10, "Hufiec", "hufiec@zhp.example.com")),
        }.ToAsyncEnumerable();
}
