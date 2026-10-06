using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyReportSender(ILogger<DummyReportSender> logger) : IReportSender
{
    public Task SendCentralReport(CertificationReport report)
    {
        logger.LogInformation("Sending central report. No of entries: {entriesCount}", report.Members.Count);
        return Task.CompletedTask;
    }

    public Task SendReport(Unit unit, IEnumerable<MemberToCertify> members)
    {
        logger.LogInformation("Sending report for unit {unit}. No of entries: {entriesCount}", unit.Name, members.Count());
        return Task.CompletedTask;
    }
}
