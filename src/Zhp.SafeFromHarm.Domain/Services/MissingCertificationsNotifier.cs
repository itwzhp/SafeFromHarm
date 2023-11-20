using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Services;

public class MissingCertificationsNotifier(
    ILogger<MissingCertificationsNotifier> logger,
    IOptions<SafeFromHarmOptions> options,
    IRequiredMembersFetcher requiredMembersFetcher,
    ICertifiedMembersFetcher certifiedMembersFetcher,
    IEmailMembershipNumberMapper numberMapper,
    INotificationSender sender,
    ISummarySender summarySender)
{
    private readonly SafeFromHarmOptions options = options.Value;

    public async Task SendNotificationsOnMissingCertificates(string? onlySendToEmail, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cerificationExpiryThreshold = DateOnly.FromDateTime(DateTime.Today).AddDays(-options.CertificateExpiryDays);
        var certifiedMembers = await certifiedMembersFetcher
            .GetCertifiedMembers()
            .Where(m => m.CertificationDate >= cerificationExpiryThreshold)
            .SelectAwait(async m => (membershipId: await numberMapper.GetMembershipNumberForEmail(m.EmailAddress), certificationDate: m.CertificationDate))
            .Where(m => !string.IsNullOrEmpty(m.membershipId))
            .GroupBy(m => m.membershipId).SelectAwait(async g => await g.FirstAsync()) //Distinct by membership id
            .ToDictionaryAsync(m => m.membershipId!, m => (DateOnly?) m.certificationDate, StringComparer.OrdinalIgnoreCase, cancellationToken);

        logger.LogInformation("Found {number} certified members", certifiedMembers.Count);

        cancellationToken.ThrowIfCancellationRequested();

        var membersWithCertInformation = await requiredMembersFetcher
            .GetMembersRequiredToCertify()
            .Select(m => (member: m, certificationDate: certifiedMembers.GetValueOrDefault(m.MembershipNumber, null)))
            .ToListAsync(cancellationToken);

        var certifiedCount = membersWithCertInformation.Count(m => m.certificationDate != null);
        var uncertifiedCount = membersWithCertInformation.Count(m => m.certificationDate == null);

        if (onlySendToEmail != null)
            membersWithCertInformation = membersWithCertInformation.Where(m => m.member.SupervisorEmail == onlySendToEmail).ToList();

        var notificationsToSend = membersWithCertInformation
            .GroupBy(m => (m.member.SupervisorEmail, m.member.SupervisorUnitName));

        var failedRecipients = new List<(string Email, string UnitName)>();

        foreach(var notification in notificationsToSend)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var groupedByCert = notification.ToLookup(n => n.certificationDate.HasValue);

            var missingCertificationMembers = groupedByCert[false].Select(m => m.member).ToList();
            var certified = groupedByCert[true].Select(m => (m.member, m.certificationDate!.Value)).ToList();
            
            logger.LogInformation("Sending notification to {supervisor} about {count} missing members and {certCount} certified", notification.Key, missingCertificationMembers.Count, certified.Count);
            try
            {
                await sender.NotifySupervisor(notification.Key.SupervisorEmail, notification.Key.SupervisorUnitName, missingCertificationMembers, certified);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Unable to send message to {unit} <{email}>", notification.Key.SupervisorUnitName, notification.Key.SupervisorEmail);
                failedRecipients.Add((notification.Key.SupervisorEmail, notification.Key.SupervisorUnitName));
            }
        }

        await summarySender.SendSummary(certifiedCount, uncertifiedCount, onlySendToEmail, failedRecipients);
    }
}
