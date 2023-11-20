using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyNotificationSender(ILogger<DummyNotificationSender> logger) : INotificationSender
{
    public Task NotifySupervisor(string supervisorEmail, string supervisorUnitMail, IEnumerable<ZhpMember> missingCertificationMembers, IEnumerable<(ZhpMember Member, DateOnly CertificationDate)> certifiedMembers)
    {
        if(logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Simulating e-mail to {supervisorUnitMail} <{supervisorEmail}>, list of missing members: {members}, list of certified members: {certMembers}",
                supervisorEmail,
                supervisorUnitMail,
                string.Join(';', missingCertificationMembers),
                string.Join(';', certifiedMembers));

        return Task.CompletedTask;
    }
}
