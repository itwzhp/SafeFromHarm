using FluentAssertions;
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
        subject = BuildSubject(new());
    }

    private SmptNotificationSender BuildSubject(SmtpOptions smtpOptions)
    {
        var factoryMock = Substitute.For<ISmtpClientFactory>();
        factoryMock.GetClient().Returns(Task.FromResult(clientMock));

        return new(Options.Create(smtpOptions), factoryMock);
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

    [Fact]
    public async Task OverrideRecipientConfigured_RecipientIsOverriden()
    {
        var subject = BuildSubject(new() { OverrideRecipient = "overriden@example.zhp.pl"});

        await subject.NotifySupervisor(
            "hufiec@zhp.example.com",
            "Hufiec",
            new ZhpMember[] { new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com", "Hufiec") });

        clientMock.ReceivedCalls().Single().GetArguments().First().Should().BeOfType<MimeMessage>()
            .Which.To.Mailboxes.Should().ContainSingle(m => m.Address == "overriden@example.zhp.pl");
    }
}
