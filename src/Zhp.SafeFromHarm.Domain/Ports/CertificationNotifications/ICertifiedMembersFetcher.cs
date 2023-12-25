using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface ICertifiedMembersFetcher
{
    IAsyncEnumerable<CertifiedMember> GetCertifiedMembers();
}
