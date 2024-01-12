﻿using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface INotificationSender
{
    Task NotifySupervisor(
        string supervisorEmail,
        string supervisorUnitName,
        IEnumerable<MemberToCertify> missingCertificationMembers,
        IEnumerable<(MemberToCertify Member, DateOnly CertificationDate)> certifiedMembers);
}
