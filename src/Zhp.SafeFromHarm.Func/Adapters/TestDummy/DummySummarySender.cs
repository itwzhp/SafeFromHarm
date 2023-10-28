using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

public class DummySummarySender : ISummarySender
{
    private readonly ILogger<DummySummarySender> logger;

    public DummySummarySender(ILogger<DummySummarySender> logger)
    {
        this.logger = logger;
    }

    public Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter, IReadOnlyCollection<(string Email, string UnitName)> failedRecipients)
    {
        logger.LogDebug("Finished sending - {nonCertifed} not certified, {certified} certified. Filter: {mailFilter}", numberOfMissingCertificates, numberOfCertifedMembers, mailFilter);
        return Task.CompletedTask;
    }
}
