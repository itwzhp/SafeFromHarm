using Zhp.SafeFromHarm.Domain.Model;

namespace Zhp.SafeFromHarm.Domain.Ports;

public interface ICertifiedMembersFetcher
{
    IAsyncEnumerable<CertifiedMember> GetCertifiedMembers();
}
