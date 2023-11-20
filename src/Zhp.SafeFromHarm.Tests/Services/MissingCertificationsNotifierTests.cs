using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model;
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
            Options.Create(new SafeFromHarmOptions()),
            new DummyRequiredMembersFetcher(),
            new DummyCertifiedMembersFetcher(),
            new DummyEmailMembershipNumberMapper(),
            senderSubstitute,
            new DummySummarySender(Substitute.For<ILogger<DummySummarySender>>()));
    }

    [Fact]
    public async Task SendNotificationsOnMissingCertificates_FindsRequiredMembers()
    {
        await subject.SendNotificationsOnMissingCertificates(null, CancellationToken.None);

        senderSubstitute.ReceivedCalls().Should().HaveCount(2);

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as string == "hufiec@zhp.example.com").GetArguments().Should().SatisfyRespectively(
            p => p.As<string>().Should().Be("hufiec@zhp.example.com"),
            p => p.As<string>().Should().Be("Hufiec"),
            p => p.As<IEnumerable<MemberToCertify>>().Should().BeEquivalentTo(new MemberToCertify[] { new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com", "Hufiec"), new("Anna", "Nowak", "AA03", "hufiec@zhp.example.com", "Hufiec") }),
            p => p.As<IEnumerable<(MemberToCertify, DateOnly)>>().Should().ContainSingle().Which.Should().Be((new MemberToCertify("Jan", "Kowalski", "AA01", "hufiec@zhp.example.com", "Hufiec"), DateOnly.FromDateTime(DateTime.Today).AddDays(-10))));

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as string == "drugihufiec@zhp.example.com").GetArguments().ElementAt(2)
            .Should().BeEquivalentTo(new MemberToCertify[] { new("Tomasz", "Innyhufiec", "AB01", "drugihufiec@zhp.example.com", "Drugi Hufiec") });
    }

    [Fact]
    public async Task SendNotificationsOnMissingCertificates_FilteredRecipient_SendsOnlyToFilteredRecipient()
    {
        await subject.SendNotificationsOnMissingCertificates("drugihufiec@zhp.example.com", CancellationToken.None);

        senderSubstitute.ReceivedCalls().Should().ContainSingle().Which.GetArguments().ElementAt(2)
            .Should().BeEquivalentTo(new MemberToCertify[] { new("Tomasz", "Innyhufiec", "AB01", "drugihufiec@zhp.example.com", "Drugi Hufiec") });
    }
}
