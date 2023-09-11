using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyNotificationSender : INotificationSender
{
    private readonly ILogger<DummyNotificationSender> logger;

    public DummyNotificationSender(ILogger<DummyNotificationSender> logger)
    {
        this.logger = logger;
    }

    public Task NotifySupervisor(string supervisorEmail, IEnumerable<ZhpMember> missingCertificationMembers)
    {
        logger.LogDebug("Simulating e-mail to {supervisorEmail}, list of members: {members}", supervisorEmail, string.Join(';', missingCertificationMembers));
        return Task.CompletedTask;
    }
}
