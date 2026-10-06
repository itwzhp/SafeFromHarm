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
    private static readonly Unit TestHufiec = new(10, "Hufiec", "hufiec@zhp.example.com");
    private static readonly Unit TestChoragiew = new(11, "Chorągiew", "choragiew@zhp.example.com");

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
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name, null),
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name, null)
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
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name, null),
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name, null)
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
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, "Drużyna Testowa", new(2023, 10, 02)),
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name, new(2023, 12, 02))
            ],
            []);

        var sentBody = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Body.As<MultipartAlternative>();
        sentBody.HtmlBody.Should().Contain("Anna Nowak (AA03) - ważny do 02.12.2023").And.Contain("Drużyna Testowa");
        sentBody.TextBody.Should().Contain("Anna Nowak (AA03) - ważny do 02.12.2023").And.Contain("Drużyna Testowa");
    }

    [Fact]
    public async Task MemberWithExpiredCertificate_ContentHasExpirationAnnotation()
    {
        await subject.NotifySupervisor(
            TestHufiec,
            [
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name, null) { Status = CertificationStatus.None },
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name, new(2026, 1, 1)) { Status = CertificationStatus.Expired }
            ],
            [],
            []);

        var sentBody = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Body.As<MultipartAlternative>();
        sentBody.HtmlBody.Should().Contain("Anna Nowak (AA03) - certyfikat wygasł 01.01.2026").And.Contain("Jan Kowalski (AA02)</li>");
        sentBody.TextBody.Should().Contain("Anna Nowak (AA03) - certyfikat wygasł 01.01.2026");
    }

    [Fact]
    public async Task MembersWithEachStatus_CsvAttachmentDistinguishesExpiredFromMissing()
    {
        await subject.NotifySupervisor(
            TestChoragiew,
            [],
            [],
            [
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name, new(2027, 10, 2)) { Status = CertificationStatus.Valid },
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name, new(2026, 1, 1)) { Status = CertificationStatus.Expired },
                new("Tomasz", "Bezcertyfikatu", "AA04", TestHufiec, TestChoragiew, TestHufiec.Name, null) { Status = CertificationStatus.None }
            ]);

        var csv = ReadCsvAttachment();

        csv.Should().ContainAll(
            "Imie, Nazwisko, Numer ewidencji, Jednostka, Przydzial, Status, Certyfikat wazny do",
            "Jan,Kowalski,AA02,Hufiec,Hufiec,ważny,2027-10-02",
            "Anna,Nowak,AA03,Hufiec,Hufiec,wygasł,2026-01-01",
            "Tomasz,Bezcertyfikatu,AA04,Hufiec,Hufiec,brak,");
    }

    private string ReadCsvAttachment()
    {
        var attachment = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Attachments.OfType<MimePart>().Single();

        using var reader = new StreamReader(attachment.Content!.Open());
        return reader.ReadToEnd();
    }

    [Fact]
    public async Task OverrideRecipientConfigured_RecipientIsOverriden()
    {
        var subject = BuildSubject(new() { OverrideRecipient = "overriden@example.zhp.pl", Username = "safe.from.harm@example.zhp.pl" });

        await subject.NotifySupervisor(
            TestHufiec,
            [new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name, null)],
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
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, TestHufiec.Name, new(2023, 10, 02)),
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, TestHufiec.Name, null)
            ]);

        var attachment = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single().Attachments.Single();
        attachment.ContentType.ToString().Should().Contain("text/csv");

        using var stream = new MemoryStream();
        attachment.WriteTo(stream, true, TestContext.Current.CancellationToken);
        stream.Length.Should().BePositive();
        stream.GetBuffer().Should().StartWith([..Encoding.ASCII.GetBytes($"{escapedBom}Imie")]);
    }
}
