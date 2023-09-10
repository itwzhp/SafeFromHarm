using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal class TipiRequiredMembersFetcher : IRequiredMembersFetcher
{
    public IAsyncEnumerable<ZhpMember> GetMembersRequiredToCertify()
    {
        throw new NotImplementedException();
    }
}
