using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

namespace Zhp.SafeFromHarm.Tests.Model;

public class CertificationReportTests
{
    private static readonly DateOnly Today = new(2026, 10, 4);
    private static readonly Unit TestHufiec = new(10, "Hufiec", "hufiec@zhp.example.com");
    private static readonly Unit TestChoragiew = new(15, "Chorągiew", "choragiew@zhp.example.com");

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, true)]
    [InlineData(-1, false)]
    [InlineData(null, false)]
    public void Constructor_CertificateValidUntilRelativeToToday_CertifiedOnlyWhenNotExpired(int? validForDays, bool expectedCertified)
    {
        DateOnly? validUntil = validForDays is int days ? Today.AddDays(days) : null;
        var member = new MemberToCertify("Jan", "Kowalski", "AA01", TestHufiec, TestChoragiew, TestHufiec.Name, validUntil);

        var report = new CertificationReport([member], Today);

        report.Members.Should().ContainSingle().Which.IsCertified.Should().Be(expectedCertified);
        report.NumberCertified.Should().Be(expectedCertified ? 1 : 0);
        report.NumberNotCertified.Should().Be(expectedCertified ? 0 : 1);
    }
}
