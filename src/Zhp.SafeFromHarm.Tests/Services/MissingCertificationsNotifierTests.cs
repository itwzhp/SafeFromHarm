using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain;
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
            new Domain.Helpers.CertificationReportProvider(
                new DummyRequiredMembersFetcher(),
                new DummyCertifiedMembersFetcher(),
                new DummyEmailMembershipNumberMapper(),
                Options.Create(new SafeFromHarmOptions()),
                Substitute.For<ILogger<Domain.Helpers.CertificationReportProvider>>()),
            senderSubstitute,
            new DummySummarySender(Substitute.For<ILogger<DummySummarySender>>()));
    }

    [Fact]
    public async Task SendNotificationsOnMissingCertificates_FindsRequiredMembers()
    {
        await subject.SendNotificationsOnMissingCertificates(null, CancellationToken.None);

        senderSubstitute.ReceivedCalls().Should().HaveCount(3);

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as string == "hufiec@zhp.example.com").GetArguments().Should().SatisfyRespectively(
            p => p.As<string>().Should().Be("hufiec@zhp.example.com"),
            p => p.As<string>().Should().Be("Hufiec"),
            p => p.As<IEnumerable<MemberToCertify>>().Should().BeEquivalentTo(new MemberToCertify[] { new("Jan", "Kowalski", "AA02", new(10, "Hufiec", "hufiec@zhp.example.com"), new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl")), new("Anna", "Nowak", "AA03", new(10, "Hufiec", "hufiec@zhp.example.com"), new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl")) }),
            p => p.As<IEnumerable<(MemberToCertify, DateOnly)>>().Should().ContainSingle().Which.Should().Be((new MemberToCertify("Jan", "Kowalski", "AA01", new(10, "Hufiec", "hufiec@zhp.example.com"), new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl")), DateOnly.FromDateTime(DateTime.Today).AddDays(-10))));

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as string == "drugihufiec@zhp.example.com").GetArguments().ElementAt(2)
            .Should().BeEquivalentTo(new MemberToCertify[]
            {
                new("Tomasz", "Innyhufiec", "AB01", new(11, "Drugi Hufiec", "drugihufiec@zhp.example.com"), new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl"))
            });

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as string == "trzecihufiec@zhp.example.com").GetArguments().ElementAt(2)
            .Should().BeEquivalentTo(new MemberToCertify[]
            {
                new("Anna", "Malinowska", "AA05", new(12, "Trzeci Hufiec", "trzecihufiec@zhp.example.com"), new(16, "Chorągiew 2", "biuro@choragiew2.zhp.pl"))
            });
    }

    [Fact]
    public async Task SendNotificationsOnMissingCertificates_FilteredRecipient_SendsOnlyToFilteredRecipient()
    {
        await subject.SendNotificationsOnMissingCertificates("drugihufiec@zhp.example.com", CancellationToken.None);

        senderSubstitute.ReceivedCalls().Should().ContainSingle().Which.GetArguments().ElementAt(2)
            .Should().BeEquivalentTo(new MemberToCertify[] { new("Tomasz", "Innyhufiec", "AB01", new(11, "Drugi Hufiec", "drugihufiec@zhp.example.com"), new(15, "Chorągiew 1", "biuro@choragiew1.zhp.pl")) });
    }
}
