using Zhp.SafeFromHarm.Domain.Model;

namespace Zhp.SafeFromHarm.Domain.Ports;

public interface IRequiredMembersFetcher
{
    IAsyncEnumerable<ZhpMember> GetMembersRequiredToCertify();
}
