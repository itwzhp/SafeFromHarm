using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Helpers;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Services;

public class MissingCertificationsNotifier(
    ILogger<MissingCertificationsNotifier> logger,
    CertificationReportProvider reportProvider,
    INotificationSender sender,
    ISummarySender summarySender)
{
    public async Task SendNotificationsOnMissingCertificates(string? onlySendToEmail, CancellationToken cancellationToken)
    {
        var originalReport = await reportProvider.GetReport(cancellationToken);

        var filteredReport = onlySendToEmail == null
            ? originalReport
            : new CertificationReport(originalReport.Entries.Where(m => m.Member.Supervisor.Email == onlySendToEmail).ToList());

        var notificationsToSend = filteredReport.Entries
            .GroupBy(m => m.Member.Supervisor);

        var failedRecipients = new List<(string Email, string UnitName)>();

        foreach(var notification in notificationsToSend)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groupedByCert = notification.ToLookup(n => n.CertificationDate.HasValue);

            var missingCertificationMembers = groupedByCert[false].Select(m => m.Member).ToList();
            var certified = groupedByCert[true].Select(m => (m.Member, m.CertificationDate!.Value)).ToList();
            
            logger.LogInformation("Sending notification to {supervisor} about {count} missing members and {certCount} certified", notification.Key, missingCertificationMembers.Count, certified.Count);
            try
            {
                await sender.NotifySupervisor(notification.Key, missingCertificationMembers, certified);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Unable to send message to {unit} <{email}>", notification.Key.Name, notification.Key.Email);
                failedRecipients.Add((notification.Key.Email, notification.Key.Name));
            }
        }

        await summarySender.SendSummary(originalReport.NumberCertified, originalReport.NumberNotCertified, onlySendToEmail, failedRecipients);
    }
}
