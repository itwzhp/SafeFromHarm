using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyRequiredMembersFetcher : IRequiredMembersFetcher
{
    private readonly Unit hufiec1 = new(10, "Hufiec", "hufiec@zhp.example.com");
    private readonly Unit hufiec2 = new(11, "Drugi Hufiec", "drugihufiec@zhp.example.com");
    private readonly Unit hufiec3 = new(12, "Trzeci Hufiec", "trzecihufiec@zhp.example.com");

    private readonly Unit choragiew1 = new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl");
    private readonly Unit choragiew2 = new(16, "Chorągiew 2", "biuro@choragiew2.zhp.pl");

    public IAsyncEnumerable<MemberToCertify> GetMembersRequiredToCertify()
        => new MemberToCertify[]
        {
            new("Jan", "Kowalski", "AA01", hufiec1, choragiew1),
            new("Jan", "Kowalski", "AA02", hufiec1, choragiew1),
            new("Tomasz", "Innyhufiec", "AB01", hufiec2, choragiew1),
            new("Anna", "Nowak", "AA03", hufiec1, choragiew1),

            new("Anna", "Malinowska", "AA05", hufiec3, choragiew2),
        }.ToAsyncEnumerable();
}
