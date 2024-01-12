using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyUnitContactMailProvider : IUnitContactMailProvider
{
    public IAsyncEnumerable<string> GetEmailAddresses(int unitId)
        => AsyncEnumerable.Empty<string>();
}
