using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface ISummarySender
{
    Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter, IReadOnlyCollection<(string Email, string UnitName)> failedRecipients);

    Task SendCentralReport(CertificationReport report, string? mailFilter, IReadOnlyCollection<Unit> failedRecipients);
}
