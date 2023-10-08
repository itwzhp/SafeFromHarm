using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Func.Adapters.Smtp;

namespace Zhp.SafeFromHarm.Tests.Adapters.Smtp;

public class SmtpNotificationSenderTests
{
    private readonly SmptNotificationSender subject;
    private readonly ISmtpClient clientMock = Substitute.For<ISmtpClient>();

    public SmtpNotificationSenderTests()
    {
        var factoryMock = Substitute.For<ISmtpClientFactory>();
        factoryMock.GetClient().Returns(Task.FromResult(clientMock));

        subject = new(Options.Create(new SmtpOptions()), factoryMock);
    }

    [Fact]
    public async Task EmptyList_DoesNothing()
    {
        await subject.NotifySupervisor("hufec@example.zhp.pl", "Hufiec", Enumerable.Empty<ZhpMember>());

        clientMock.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task SeveralItems_BuildsContent()
    {
        await subject.NotifySupervisor(
            "hufiec@zhp.example.com",
            "Hufiec",
            new ZhpMember[]
            {
                new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com", "Hufiec"),
                new("Anna", "Nowak", "AA03", "hufiec@zhp.example.com", "Hufiec")
            });

        await clientMock.Received().SendAsync(Arg.Is<MimeMessage>(m => ((TextPart)m.Body).Text.Contains("Anna Nowak (AA03)")));
    }
}
