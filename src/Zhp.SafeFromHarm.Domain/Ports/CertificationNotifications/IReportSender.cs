using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface IReportSender
{
    Task SendReport(Unit unit, IEnumerable<CertificationReport.ReportEntry> entries);
}
