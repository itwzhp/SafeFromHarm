using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Services;

public class ReportGenerator(
    IRequiredMembersFetcher membersFetcher,
    IReportSender sender,
    ISummarySender summarySender)
{
    const int HeadquartersId = 2;

    public async Task SendReports(string? onlySendToEmail, CancellationToken cancellationToken)
    {
        var members = await membersFetcher.GetMembersRequiredToCertify().ToListAsync(cancellationToken);
        var reports = new CertificationReport(members, DateOnly.FromDateTime(DateTime.Today));

        cancellationToken.ThrowIfCancellationRequested();

        var reportsToSend = reports.Members
            .Where(m => m.Department.Id != HeadquartersId)
            .GroupBy(m => m.Department);

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
