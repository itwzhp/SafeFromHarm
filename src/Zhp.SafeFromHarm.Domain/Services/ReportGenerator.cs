using Zhp.SafeFromHarm.Domain.Helpers;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Services;

public class ReportGenerator(
    CertificationReportProvider reportProvider,
    IReportSender sender,
    ISummarySender summarySender)
{
    const int HeadquartersId = 2;
    
    public async Task SendReports(string? onlySendToEmail, CancellationToken cancellationToken)
    {
        var originalReport = await reportProvider.GetReport(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var filteredReport = onlySendToEmail == null
           ? originalReport
           : new CertificationReport(originalReport.Entries.Where(m => m.Member.Department.Email == onlySendToEmail).ToList());

        var reportsToSend = filteredReport.Entries
            .Where(m => m.Member.Department.Id != HeadquartersId)
            .GroupBy(m => m.Member.Department);

        List<Unit> failedRecipients = [];

        foreach (var report in reportsToSend)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await sender.SendReport(report.Key, report);
            }
            catch
            {
                failedRecipients.Add(report.Key);
            }
        }

        await sender.SendCentralReport(originalReport);
        
        cancellationToken.ThrowIfCancellationRequested();

        await summarySender.SendCentralReport(originalReport, onlySendToEmail, failedRecipients);
    }
}
