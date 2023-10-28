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
        subject = BuildSubject(new() { Username = "safe.from.harm@example.zhp.pl" });
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
        await subject.NotifySupervisor("hufec@example.zhp.pl", "Hufiec", Enumerable.Empty<ZhpMember>(), Enumerable.Empty<(ZhpMember, DateOnly)>());

        clientMock.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task SeveralPeopleToCertify_BuildsContent()
    {
        await subject.NotifySupervisor(
            "hufiec@zhp.example.com",
            "Hufiec",
            new ZhpMember[]
            {
                new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com", "Hufiec"),
                new("Anna", "Nowak", "AA03", "hufiec@zhp.example.com", "Hufiec")
            },
            Enumerable.Empty<(ZhpMember, DateOnly)>());

        var sentBody = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Body.As<MultipartAlternative>();
        sentBody.HtmlBody.Should().Contain("Anna Nowak (AA03)");
        sentBody.TextBody.Should().Contain("Anna Nowak (AA03)");
    }
    
    [Fact]
    public async Task SeveralPeopleToCertify_PlainTextHasCorrectLinks()
    {
        await subject.NotifySupervisor(
            "hufiec@zhp.example.com",
            "Hufiec",
            new ZhpMember[]
            {
                new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com", "Hufiec"),
                new("Anna", "Nowak", "AA03", "hufiec@zhp.example.com", "Hufiec")
            },
            Enumerable.Empty<(ZhpMember, DateOnly)>());

        var sentBody = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Body.As<MultipartAlternative>();

        sentBody.TextBody.Should().Contain("Harcerskim Serwisie Szkoleniowym (https://edu.zhp.pl/course/view.php?id=47)");
        sentBody.TextBody.Should().NotContainAny("<", ">");
    }

    [Fact]
    public async Task SeveralPeopleCertified_BuildsContent()
    {
        await subject.NotifySupervisor(
            "hufiec@zhp.example.com",
            "Hufiec",
            Enumerable.Empty<ZhpMember>(),
            new (ZhpMember, DateOnly)[]
            {
                (new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com", "Hufiec"), new(2023, 10, 02)),
                (new("Anna", "Nowak", "AA03", "hufiec@zhp.example.com", "Hufiec"), new(2023, 12, 02))
            });

        var sentBody = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Body.As<MultipartAlternative>();
        sentBody.HtmlBody.Should().Contain("Anna Nowak (AA03) - 02.12.2023");
        sentBody.TextBody.Should().Contain("Anna Nowak (AA03) - 02.12.2023");
    }

    [Fact]
    public async Task OverrideRecipientConfigured_RecipientIsOverriden()
    {
        var subject = BuildSubject(new() { OverrideRecipient = "overriden@example.zhp.pl", Username = "safe.from.harm@example.zhp.pl" });

        await subject.NotifySupervisor(
            "hufiec@zhp.example.com",
            "Hufiec",
            new ZhpMember[] { new("Jan", "Kowalski", "AA02", "hufiec@zhp.example.com", "Hufiec") },
            Enumerable.Empty<(ZhpMember, DateOnly)>());

        clientMock.ReceivedCalls().Single().GetArguments().First().Should().BeOfType<MimeMessage>()
            .Which.To.Mailboxes.Should().ContainSingle(m => m.Address == "overriden@example.zhp.pl");
    }
}
