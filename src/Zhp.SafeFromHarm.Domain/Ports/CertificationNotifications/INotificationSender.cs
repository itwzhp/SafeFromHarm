using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface INotificationSender
{
    Task NotifySupervisor(
        Unit supervisor,
        IEnumerable<MemberToCertify> missingCertificationMembers,
        IEnumerable<CertifiedMember> certifiedMembers,
        IEnumerable<CertificationReport.ReportEntry> allMembersIncludingSubunits);
}

public record CertifiedMember(MemberToCertify Member, DateOnly CertificationDate);
