using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Func.Adapters.Smtp;
using Zhp.SafeFromHarm.Func.Adapters.TestDummy;

namespace Zhp.SafeFromHarm.Tests.Adapters.Smtp;

public class SmtpNotificationSenderTests
{
    private static readonly Unit TestHufiec = new(10, "hufiec@zhp.example.com", "Hufiec");
    private static readonly Unit TestChoragiew = new(11, "choragiew@zhp.example.com", "Chorągiew");

    private readonly SmtpNotificationSender subject;
    private readonly ISmtpClient clientMock = Substitute.For<ISmtpClient>();

    public SmtpNotificationSenderTests()
    {
        subject = BuildSubject(new() { Username = "safe.from.harm@example.zhp.pl" });
    }

    private SmtpNotificationSender BuildSubject(SmtpOptions smtpOptions)
    {
        var factoryMock = Substitute.For<ISmtpClientFactory>();
        factoryMock.GetClient().Returns(Task.FromResult(clientMock));

        return new(Options.Create(smtpOptions), factoryMock, new DummyUnitContactMailProvider());
    }

    [Fact]
    public async Task EmptyList_DoesNothing()
    {
        await subject.NotifySupervisor(TestHufiec, [], [], []);

        clientMock.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task SeveralPeopleToCertify_BuildsContent()
    {
        await subject.NotifySupervisor(
            TestHufiec,
            [
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name),
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name)
            ],
            [],
            []);

        var sentBody = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Body.As<MultipartAlternative>();
        sentBody.HtmlBody.Should().Contain("Anna Nowak (AA03)");
        sentBody.TextBody.Should().Contain("Anna Nowak (AA03)");
    }
    
    [Fact]
    public async Task SeveralPeopleToCertify_PlainTextHasCorrectLinks()
    {
        await subject.NotifySupervisor(
            TestHufiec,
            [
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name),
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name)
            ],
            [],
            []);

        var sentBody = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Body.As<MultipartAlternative>();

        sentBody.TextBody.Should().Contain("Harcerskim Serwisie Szkoleniowym (https://edu.zhp.pl/course/view.php?id=47)");
        sentBody.TextBody.Should().NotContainAny("<", ">");
    }

    [Fact]
    public async Task SeveralPeopleCertified_BuildsContent()
    {
        await subject.NotifySupervisor(
            TestHufiec,
            [],
            [
                new (new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, "Drużyna Testowa"), new(2023, 10, 02)),
                new (new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name), new(2023, 12, 02))
            ],
            []);

        var sentBody = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Body.As<MultipartAlternative>();
        sentBody.HtmlBody.Should().Contain("Anna Nowak (AA03) - 02.12.2023").And.Contain("Drużyna Testowa");
        sentBody.TextBody.Should().Contain("Anna Nowak (AA03) - 02.12.2023").And.Contain("Drużyna Testowa");
    }

    [Fact]
    public async Task OverrideRecipientConfigured_RecipientIsOverriden()
    {
        var subject = BuildSubject(new() { OverrideRecipient = "overriden@example.zhp.pl", Username = "safe.from.harm@example.zhp.pl" });

        await subject.NotifySupervisor(
            TestHufiec,
            [new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name)],
            [],
            []);

        clientMock.ReceivedCalls().Single().GetArguments().First().Should().BeOfType<MimeMessage>()
            .Which.To.Mailboxes.Should().ContainSingle(m => m.Address == "overriden@example.zhp.pl");
    }

    [Fact]
    public async Task MembersInAllCertificationMembers_AddedToCsvAttachment()
    {
        const string escapedBom = "=EF=BB=BF";

        await subject.NotifySupervisor(
            TestChoragiew,
            [],
            [],
            [
                new (new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name), new(2023, 10, 02)),
                new (new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name    ), null)
            ]);

        var attachment = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single().Attachments.Single();
        attachment.ContentType.ToString().Should().Contain("text/csv");

        using var stream = new MemoryStream();
        attachment.WriteTo(stream, true);
        stream.Length.Should().BePositive();
        stream.GetBuffer().Should().StartWith([..Encoding.ASCII.GetBytes($"{escapedBom}Imie")]);
    }
}
