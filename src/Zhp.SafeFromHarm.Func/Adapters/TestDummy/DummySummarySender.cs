using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

public class DummySummarySender(ILogger<DummySummarySender> logger) : ISummarySender
{
    public Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter, IReadOnlyCollection<(string Email, string UnitName)> failedRecipients)
    {
        logger.LogDebug("Finished sending - {nonCertifed} not certified, {certified} certified. Filter: {mailFilter}", numberOfMissingCertificates, numberOfCertifedMembers, mailFilter);
        return Task.CompletedTask;
    }
}
