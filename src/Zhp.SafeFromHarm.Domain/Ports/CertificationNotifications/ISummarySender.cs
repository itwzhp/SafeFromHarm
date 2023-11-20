namespace Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

public interface ISummarySender
{
    Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter, IReadOnlyCollection<(string Email, string UnitName)> failedRecipients);
}
