using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;

namespace Zhp.SafeFromHarm.Tests.Model;

public class CertificationReportTests
{
    private static readonly DateOnly Today = new(2026, 10, 4);
    private static readonly Unit TestHufiec = new(10, "Hufiec", "hufiec@zhp.example.com");
    private static readonly Unit TestChoragiew = new(15, "Chorągiew", "choragiew@zhp.example.com");

    [Theory]
    [InlineData(1, CertificationStatus.Valid)]
    [InlineData(0, CertificationStatus.Valid)]
    [InlineData(-1, CertificationStatus.Expired)]
    [InlineData(null, CertificationStatus.None)]
    public void Constructor_CertificateValidUntilRelativeToToday_SetsStatus(int? validForDays, CertificationStatus expectedStatus)
    {
        DateOnly? validUntil = validForDays is int days ? Today.AddDays(days) : null;
        var member = new MemberToCertify("Jan", "Kowalski", "AA01", TestHufiec, TestChoragiew, TestHufiec.Name, validUntil);

        var report = new CertificationReport([member], Today);

        var expectedCertified = expectedStatus is CertificationStatus.Valid;
        var reportedMember = report.Members.Should().ContainSingle().Which;
        reportedMember.Status.Should().Be(expectedStatus);
        reportedMember.IsCertified.Should().Be(expectedCertified);
        report.NumberCertified.Should().Be(expectedCertified ? 1 : 0);
        report.NumberNotCertified.Should().Be(expectedCertified ? 0 : 1);
    }

    [Fact]
    public void Constructor_ExpiredCertificate_KeepsExpirationDate()
    {
        var expiredOn = Today.AddDays(-1);
        var member = new MemberToCertify("Jan", "Kowalski", "AA01", TestHufiec, TestChoragiew, TestHufiec.Name, expiredOn);

        var report = new CertificationReport([member], Today);

        report.Members.Should().ContainSingle().Which.CertificateValidUntil.Should().Be(expiredOn);
    }
}
