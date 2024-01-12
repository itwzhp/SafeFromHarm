namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface IUnitContactMailProvider
{
    IAsyncEnumerable<string> GetEmailAddresses(int unitId);
}
