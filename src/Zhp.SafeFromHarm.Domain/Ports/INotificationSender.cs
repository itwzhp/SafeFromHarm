using Zhp.SafeFromHarm.Domain.Model;

namespace Zhp.SafeFromHarm.Domain.Ports;

public interface INotificationSender
{
    Task NotifySupervisor(
        string supervisorEmail,
        string supervisorUnitName,
        IEnumerable<ZhpMember> missingCertificationMembers,
        IEnumerable<(ZhpMember Member, DateOnly CertificationDate)> certifiedMembers);
}
