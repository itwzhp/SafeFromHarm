namespace Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

public record CertificationReport
{
    /// <param name="today">Certyfikat ważny do dnia wcześniejszego niż ten traktujemy jak brak certyfikatu</param>
    public CertificationReport(IEnumerable<MemberToCertify> members, DateOnly today)
    {
        Members = members
            .Select(m => m.CertificateValidUntil < today ? m with { CertificateValidUntil = null } : m)
            .ToList();

        NumberToCertify = Members.Count;
        NumberCertified = Members.Count(x => x.IsCertified);
        NumberNotCertified = Members.Count(x => !x.IsCertified);
    }

    public IReadOnlyCollection<MemberToCertify> Members { get; }

    public int NumberToCertify { get; }

    public int NumberCertified { get; }

    public int NumberNotCertified { get; }
}
