using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Services;

public class MissingCertificationsNotifier(
    ILogger<MissingCertificationsNotifier> logger,
    IRequiredMembersFetcher membersFetcher,
    INotificationSender sender,
    ISummarySender summarySender)
{
    public async Task SendNotificationsOnMissingCertificates(string? onlySendToEmail, CancellationToken cancellationToken)
    {
        var members = await membersFetcher.GetMembersRequiredToCertify().ToListAsync(cancellationToken);
        var report = new CertificationReport(members, DateOnly.FromDateTime(DateTime.Today));

        logger.LogInformation(
            "Found {number} members to certify - {certified} certified and {notCertified} not certified",
            report.NumberToCertify,
            report.NumberCertified,
            report.NumberNotCertified);

        var notificationsToSend = report.Members
            .GroupBy(m => (m.Supervisor, m.Department));

        var membersPerDepartment = report.Members
            .ToLookup(m => m.Department);

        var failedRecipients = new List<(string Email, string UnitName)>();

        foreach(var notification in notificationsToSend)
        {
            if(onlySendToEmail != null && notification.Key.Supervisor.Email != onlySendToEmail)
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            var groupedByCert = notification.ToLookup(n => n.IsCertified);

            var missingCertificationMembers = groupedByCert[false].ToList();
            var certified = groupedByCert[true].ToList();
            
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
