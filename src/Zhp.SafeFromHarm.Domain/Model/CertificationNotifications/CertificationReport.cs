namespace Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

public record CertificationReport(IReadOnlyCollection<CertificationReport.ReportEntry> Entries)
{
    public IReadOnlyCollection<ReportEntry> Entries { get; } = Entries;

    public int NumberToCertify { get; } = Entries.Count;

    public int NumberCertified { get; } = Entries.Count(x => x.CertificationDate is not null);

    public int NumberNotCertified { get; } = Entries.Count(x => x.CertificationDate is null);

    public record ReportEntry(MemberToCertify Member, DateOnly? CertificationDate);
}
