using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyEmailMembershipNumberMapper : IEmailMembershipNumberMapper
{
    public ValueTask<string?> GetMembershipNumberForEmail(string email)
        => ValueTask.FromResult(email switch
        {
            "jan.kowalski@zhp.example.com" => "AA01",
            "j.kowalski" => "AA02",
            "anna.nowak@zhp.example.com" => "AA03",
            _ => null
        });
}
