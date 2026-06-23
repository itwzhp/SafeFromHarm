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
        var reports = await reportProvider.GetReport(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var reportsToSend = reports.Entries
            .Where(m => m.Member.Department.Id != HeadquartersId)
            .GroupBy(m => m.Member.Department);

        List<Unit> failedRecipients = [];

        foreach (var report in reportsToSend)
        {
            if (onlySendToEmail != null && report.Key.Email != onlySendToEmail)
                continue;

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

        if (onlySendToEmail == null)
            await sender.SendCentralReport(reports);
        
        cancellationToken.ThrowIfCancellationRequested();

        await summarySender.SendCentralReport(reports, onlySendToEmail, failedRecipients);
    }
}
