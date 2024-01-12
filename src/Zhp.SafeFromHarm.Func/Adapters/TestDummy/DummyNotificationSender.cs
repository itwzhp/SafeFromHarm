using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyNotificationSender(ILogger<DummyNotificationSender> logger) : INotificationSender
{
    public Task NotifySupervisor(Unit supervisor, IEnumerable<MemberToCertify> missingCertificationMembers, IEnumerable<(MemberToCertify Member, DateOnly CertificationDate)> certifiedMembers)
    {
        if(logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Simulating e-mail to {supervisorUnitMail} <{supervisorEmail}>, list of missing members: {members}, list of certified members: {certMembers}",
                supervisor.Email,
                supervisor.Email,
                string.Join(';', missingCertificationMembers),
                string.Join(';', certifiedMembers));

        return Task.CompletedTask;
    }
}
