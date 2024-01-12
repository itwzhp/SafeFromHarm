using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyReportSender(ILogger<DummyReportSender> logger) : IReportSender
{
    public Task SendReport(Unit unit, IEnumerable<CertificationReport.ReportEntry> entries)
    {
        logger.LogInformation("Sending report for unit {unit}. No of entries: {entriesCount}", unit.Name, entries.Count());
        return Task.CompletedTask;
    }
}
