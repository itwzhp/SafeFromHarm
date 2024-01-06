using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Helpers;

public class CertificationReportProvider(
    IRequiredMembersFetcher requiredMembersFetcher,
    ICertifiedMembersFetcher certifiedMembersFetcher,
    IEmailMembershipNumberMapper numberMapper,
    IOptions<SafeFromHarmOptions> options,
    ILogger<CertificationReportProvider> logger)
{
    public async Task<CertificationReport> GetReport(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cerificationExpiryThreshold = DateOnly.FromDateTime(DateTime.Today).AddDays(-options.Value.CertificateExpiryDays);
        var certifiedMembers = await certifiedMembersFetcher
            .GetCertifiedMembers()
            .Where(m => m.CertificationDate >= cerificationExpiryThreshold)
            .SelectAwait(async m => (membershipId: await numberMapper.GetMembershipNumberForEmail(m.EmailAddress), certificationDate: m.CertificationDate))
            .Where(m => !string.IsNullOrEmpty(m.membershipId))
            .GroupBy(m => m.membershipId).SelectAwait(async g => await g.FirstAsync()) //Distinct by membership id
            .ToDictionaryAsync(m => m.membershipId!, m => (DateOnly?)m.certificationDate, StringComparer.OrdinalIgnoreCase, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var reportEntries = await requiredMembersFetcher
            .GetMembersRequiredToCertify()
            .Select(m => new CertificationReport.ReportEntry(m, certifiedMembers.GetValueOrDefault(m.MembershipNumber, null)))
            .ToListAsync(cancellationToken);

        var report = new CertificationReport(reportEntries);

        logger.LogInformation(
            "Found {number} certified members - {certified} certified and {notCertified} not certified",
            report.NumberToCertify,
            report.NumberCertified,
            report.NumberNotCertified);

        return report;
    }
}
