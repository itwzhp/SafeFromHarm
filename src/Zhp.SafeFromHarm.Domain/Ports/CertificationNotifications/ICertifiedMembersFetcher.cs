using Zhp.SafeFromHarm.Domain.Model;

namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface ICertifiedMembersFetcher
{
    IAsyncEnumerable<CertifiedMember> GetCertifiedMembers();
}
