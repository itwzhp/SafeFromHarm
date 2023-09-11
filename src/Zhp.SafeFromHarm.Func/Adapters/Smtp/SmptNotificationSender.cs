using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmptNotificationSender : INotificationSender
{
    public Task NotifySupervisor(string supervisorEmail, IEnumerable<ZhpMember> missingCertificationMembers)
    {
        throw new NotImplementedException();
    }
}
