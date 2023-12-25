using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyCertifiedMembersFetcher : ICertifiedMembersFetcher
{
    public IAsyncEnumerable<CertifiedMember> GetCertifiedMembers()
        => new CertifiedMember[]
        {
            new("jan.kowalski@zhp.example.com", DateOnly.FromDateTime(DateTime.Today).AddDays(-10)),
            new("t.nowak@zhp.example.com", DateOnly.FromDateTime(DateTime.Today).AddDays(-20)),
            new("anna.nowak@zhp.example.com", DateOnly.FromDateTime(DateTime.Today).AddYears(-5)),
        }.ToAsyncEnumerable();
}
