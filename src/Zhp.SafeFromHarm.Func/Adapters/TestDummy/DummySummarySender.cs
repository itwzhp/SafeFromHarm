using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

public class DummySummarySender : ISummarySender
{
    private readonly Logger<DummySummarySender> logger;

    public DummySummarySender(Logger<DummySummarySender> logger)
    {
        this.logger = logger;
    }

    public Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter)
    {
        logger.LogDebug("Finished sending - {nonCertifed} not certified, {certified} certified. Filter: {mailFilter}", numberOfMissingCertificates, numberOfCertifedMembers, mailFilter);
        return Task.CompletedTask;
    }
}
