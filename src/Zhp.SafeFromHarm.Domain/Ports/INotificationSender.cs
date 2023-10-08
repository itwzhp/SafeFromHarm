using Zhp.SafeFromHarm.Domain.Model;

namespace Zhp.SafeFromHarm.Domain.Ports;

public interface INotificationSender
{
    Task NotifySupervisor(string supervisorEmail, string supervisorUnitMail, IEnumerable<ZhpMember> missingCertificationMembers);
}
