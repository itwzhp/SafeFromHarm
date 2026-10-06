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
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return new MemberToCertify[]
        {
            new("Jan", "Kowalski", "AA01", hufiec1, choragiew1, hufiec1.Name, today.AddYears(1)),
            new("Jan", "Kowalski", "AA02", hufiec1, choragiew1, hufiec1.Name, null),
            new("Tomasz", "Innyhufiec", "AB01", hufiec2, choragiew1, "Drużyna testowa", null),
            new("Anna", "Nowak", "AA03", hufiec1, choragiew1, hufiec1.Name, today.AddMonths(-1)),

            new("Anna", "Malinowska", "AA05", hufiec3, choragiew2, hufiec3.Name, null),

            new("Anna", "Abacka", "AA05", choragiew1, choragiew1, choragiew1.Name, null),
            new("Anna", "Cabacka", "AA05", choragiew1, choragiew1, choragiew1.Name, null),
        }.ToAsyncEnumerable();
    }
}
