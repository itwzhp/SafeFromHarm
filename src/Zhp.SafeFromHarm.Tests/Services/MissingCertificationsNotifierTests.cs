using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;
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
            new() { CertificateExpiryDays = 1095 },
            new DummyRequiredMembersFetcher(),
            new DummyCertifiedMembersFetcher(),
            new DummyEmailMembershipNumberMapper(),
            senderSubstitute);
    }

    [Fact]
    public async Task SendNotificationsOnMissingCertificates_FindsRequiredMembers()
    {
        await subject.SendNotificationsOnMissingCertificates(CancellationToken.None);

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as string == "hufiec@zhp.example.com").GetArguments().ElementAt(1)
            .Should().BeEquivalentTo(new ZhpMember[] { new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com"), new("Anna", "Nowak", "AA03", "hufiec@zhp.example.com") });

        senderSubstitute.ReceivedCalls().Single(c => c.GetArguments().First() as string == "drugihufiec@zhp.example.com").GetArguments().ElementAt(1)
            .Should().BeEquivalentTo(new ZhpMember[] { new("Tomasz", "Innyhufiec", "AB01", "drugihufiec@zhp.example.com") });
    }
}
