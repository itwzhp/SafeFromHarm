using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Services;
using Zhp.SafeFromHarm.Func.Adapters.TestDummy;

namespace Zhp.SafeFromHarm.Tests.Services;

public class MissingCertificationsNotifierTests
{
    private readonly INotificationSender senderSubstitute = Substitute.For<INotificationSender>();
    private readonly MissingCertificationsNotifier subject;

    public MissingCertificationsNotifierTests()
    {
        subject = new(
            Substitute.For<ILogger<MissingCertificationsNotifier>>(),
            new DummyRequiredMembersFetcher(),
            senderSubstitute,
            new DummySummarySender(Substitute.For<ILogger<DummySummarySender>>()));
    }

    [Fact]
    public async Task SendNotificationsOnMissingCertificates_FindsRequiredMembers()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var testUnit1 = new Unit(10, "Hufiec", "hufiec@zhp.example.com");
        var testUnit2 = new Unit(11, "Drugi Hufiec", "drugihufiec@zhp.example.com");
        var testUnit3 = new Unit(12, "Trzeci Hufiec", "trzecihufiec@zhp.example.com");
        var testUnit4 = new Unit(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl");
        await subject.SendNotificationsOnMissingCertificates(null, CancellationToken.None);

        senderSubstitute.ReceivedCalls().Should().HaveCount(4);

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as Unit == testUnit1).GetArguments().Should().SatisfyRespectively(
            p => p.As<Unit>().Should().Be(testUnit1),
            p => p.As<IEnumerable<MemberToCertify>>().Should().BeEquivalentTo(new MemberToCertify[]
            {
                new("Jan", "Kowalski", "AA02", testUnit1, new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl"), testUnit1.Name, null) { Status = CertificationStatus.None },
                new("Anna", "Nowak", "AA03", testUnit1, new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl"), testUnit1.Name, today.AddMonths(-1)) { Status = CertificationStatus.Expired }
            }),
            p => p.As<IEnumerable<MemberToCertify>>().Should().ContainSingle().Which.Should().Be(new MemberToCertify("Jan", "Kowalski", "AA01", testUnit1, new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl"), testUnit1.Name, today.AddYears(1)) { Status = CertificationStatus.Valid }),
            p => p.As<IEnumerable<MemberToCertify>>().Should().BeEmpty());

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as Unit == testUnit2).GetArguments().ElementAt(1)
            .Should().BeEquivalentTo(new MemberToCertify[]
            {
                new("Tomasz", "Innyhufiec", "AB01", testUnit2, new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl"), "Drużyna testowa", null)
            });

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as Unit == testUnit3).GetArguments().ElementAt(1)
            .Should().BeEquivalentTo(new MemberToCertify[]
            {
                new("Anna", "Malinowska", "AA05", testUnit3, new(16, "Chorągiew 2", "biuro@choragiew2.zhp.pl"), testUnit3.Name, null)
            });

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as Unit == testUnit4).GetArguments().ElementAt(3)
            .As<IEnumerable<MemberToCertify>>().Should().HaveCount(6);
    }

    [Fact]
    public async Task SendNotificationsOnMissingCertificates_FilteredRecipient_SendsOnlyToFilteredRecipient()
    {
        await subject.SendNotificationsOnMissingCertificates("drugihufiec@zhp.example.com", CancellationToken.None);

        senderSubstitute.ReceivedCalls().Should().ContainSingle().Which.GetArguments().ElementAt(1)
            .Should().BeEquivalentTo(new MemberToCertify[] { new("Tomasz", "Innyhufiec", "AB01", new(11, "Drugi Hufiec", "drugihufiec@zhp.example.com"), new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl"), "Drużyna testowa", null) });
    }
}
