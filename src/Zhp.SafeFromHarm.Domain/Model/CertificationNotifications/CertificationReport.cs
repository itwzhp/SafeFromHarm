namespace Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

public record CertificationReport
{
    /// <param name="today">Certyfikat ważny do dnia wcześniejszego niż ten uznajemy za wygasły</param>
    public CertificationReport(IEnumerable<MemberToCertify> members, DateOnly today)
    {
        Members = members
            .Select(m => m with { Status = EvaluateStatus(m.CertificateValidUntil, today) })
            .ToList();

        NumberToCertify = Members.Count;
        NumberCertified = Members.Count(x => x.IsCertified);
        NumberNotCertified = Members.Count(x => !x.IsCertified);
    }

    public IReadOnlyCollection<MemberToCertify> Members { get; }

    public int NumberToCertify { get; }

    public int NumberCertified { get; }

    public int NumberNotCertified { get; }

    private static CertificationStatus EvaluateStatus(DateOnly? certificateValidUntil, DateOnly today)
        => certificateValidUntil switch
        {
            null => CertificationStatus.None,
            var validUntil when validUntil < today => CertificationStatus.Expired,
            _ => CertificationStatus.Valid,
        };
}
