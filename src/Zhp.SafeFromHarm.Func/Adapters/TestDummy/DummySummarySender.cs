using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

public class DummySummarySender(ILogger<DummySummarySender> logger) : ISummarySender
{
    public Task SendCentralReport(CertificationReport report, string? mailFilter, IReadOnlyCollection<Unit> failedRecipients)
    {
        logger.LogDebug("Sent full central report - {certified} certified, {nonCertifed} not certified", report.NumberCertified, report.NumberNotCertified);
        return Task.CompletedTask;
    }

    public Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter, IReadOnlyCollection<(string Email, string UnitName)> failedRecipients)
    {
        logger.LogDebug("Finished sending - {nonCertifed} not certified, {certified} certified. Filter: {mailFilter}", numberOfMissingCertificates, numberOfCertifedMembers, mailFilter);
        return Task.CompletedTask;
    }
}
