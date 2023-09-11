using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Domain.Services;

public class MissingCertificationsNotifier
{
    private readonly ILogger<MissingCertificationsNotifier> logger;
    private readonly SafeFromHarmOptions options;
    private readonly IRequiredMembersFetcher requiredMembersFetcher;
    private readonly ICertifiedMembersFetcher certifiedMembersFetcher;
    private readonly IEmailMembershipNumberMapper numberMapper;
    private readonly INotificationSender sender;

    public MissingCertificationsNotifier(
        ILogger<MissingCertificationsNotifier> logger,
        SafeFromHarmOptions options,
        IRequiredMembersFetcher requiredMembersFetcher,
        ICertifiedMembersFetcher certifiedMembersFetcher,
        IEmailMembershipNumberMapper numberMapper,
        INotificationSender sender)
    {
        this.logger = logger;
        this.options = options;
        this.requiredMembersFetcher = requiredMembersFetcher;
        this.certifiedMembersFetcher = certifiedMembersFetcher;
        this.numberMapper = numberMapper;
        this.sender = sender;
    }

    public async Task SendNotificationsOnMissingCertificates(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cerificationExpiryThreshold = DateOnly.FromDateTime(DateTime.Today).AddDays(-options.CertificateExpiryDays);
        var certifiedMembers = await certifiedMembersFetcher
            .GetCertifiedMembers()
            .Where(m => m.CertificationDate >= cerificationExpiryThreshold)
            .SelectAwait(async m => await numberMapper.GetMembershipNumberForEmail(m.EmailAddress))
            .OfType<string>()
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, cancellationToken);
        logger.LogDebug("Found {number} certified members", certifiedMembers.Count);

        cancellationToken.ThrowIfCancellationRequested();

        var missingCertifications = requiredMembersFetcher
            .GetMembersRequiredToCertify()
            .Where(m => !certifiedMembers.Contains(m.MembershipNumber));

        var notificationsToSend = missingCertifications
            .GroupBy(m => m.SupervisorEmail);

        await foreach(var notification in notificationsToSend)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var missingCertificationMembers = await notification.ToListAsync(cancellationToken);
            
            logger.LogDebug("Sending notification to {supervisor} about {count} missing members", notification.Key, missingCertificationMembers.Count);
            await sender.NotifySupervisor(notification.Key, missingCertificationMembers);
        }
    }
}
