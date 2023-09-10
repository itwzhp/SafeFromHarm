using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle;

internal class MoodleEmailMembershipNumberMapper : IEmailMembershipNumberMapper
{
    public ValueTask<string?> GetMembershipNumberForEmail(string email)
    {
        throw new NotImplementedException();
    }
}
