using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Helpers;
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
        var report = await reportProvider.GetReport(cancellationToken);

        var notificationsToSend = report.Entries
            .GroupBy(m => (m.Member.Supervisor, m.Member.Department));

        var membersPerDepartment = report.Entries
            .ToLookup(m => m.Member.Department);

        var failedRecipients = new List<(string Email, string UnitName)>();

        foreach(var notification in notificationsToSend)
        {
            if(onlySendToEmail != null && notification.Key.Supervisor.Email != onlySendToEmail)
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            var groupedByCert = notification.ToLookup(n => n.CertificationDate.HasValue);

            var missingCertificationMembers = groupedByCert[false].Select(m => m.Member).ToList();
            var certified = groupedByCert[true].Select(m => new CertifiedMember(m.Member, m.CertificationDate!.Value)).ToList();
            
            logger.LogInformation("Sending notification to {supervisor} about {count} missing members and {certCount} certified", notification.Key, missingCertificationMembers.Count, certified.Count);
            try
            {
                await sender.NotifySupervisor(notification.Key.Supervisor, missingCertificationMembers, certified, membersPerDepartment[notification.Key.Supervisor]);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Unable to send message to {unit} <{email}>", notification.Key.Supervisor.Name, notification.Key.Supervisor.Email);
                failedRecipients.Add((notification.Key.Supervisor.Email, notification.Key.Supervisor.Name));
            }
        }

        await summarySender.SendSummary(report.NumberCertified, report.NumberNotCertified, onlySendToEmail, failedRecipients);
    }
}
