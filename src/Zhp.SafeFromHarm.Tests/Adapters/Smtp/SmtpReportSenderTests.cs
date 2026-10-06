using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Func.Adapters.Smtp;
using Zhp.SafeFromHarm.Func.Adapters.TestDummy;

namespace Zhp.SafeFromHarm.Tests.Adapters.Smtp;

public class SmtpReportSenderTests
{
    private static readonly Unit TestHufiec = new(10, "Hufiec", "hufiec@zhp.example.com");
    private static readonly Unit TestChoragiew = new(11, "Chorągiew", "choragiew@zhp.example.com");

    private readonly ISmtpClient clientMock = Substitute.For<ISmtpClient>();
    private readonly SmtpReportSender subject;

    public SmtpReportSenderTests()
    {
        var factoryMock = Substitute.For<ISmtpClientFactory>();
        factoryMock.GetClient().Returns(Task.FromResult(clientMock));

        subject = new(factoryMock, Options.Create(new SmtpOptions { Username = "safe.from.harm@example.zhp.pl" }), new DummyUnitContactMailProvider());
    }

    [Fact]
    public async Task MembersWithEachStatus_CsvAttachmentDistinguishesExpiredFromMissing()
    {
        await subject.SendReport(
            TestChoragiew,
            [
                new("Jan", "Kowalski", "AA02", TestHufiec, TestChoragiew, "Drużyna Testowa", new(2027, 10, 2)) { Status = CertificationStatus.Valid },
                new("Anna", "Nowak", "AA03", TestHufiec, TestChoragiew, "Drużyna Testowa", new(2026, 1, 1)) { Status = CertificationStatus.Expired },
                new("Tomasz", "Bezcertyfikatu", "AA04", TestHufiec, TestChoragiew, "Drużyna Testowa", null) { Status = CertificationStatus.None }
            ]);

        var attachment = clientMock.ReceivedCalls().Single().GetArguments().OfType<MimeMessage>().Single()
            .Attachments.OfType<MimePart>().Single();
        using var reader = new StreamReader(attachment.Content!.Open());
        var csv = reader.ReadToEnd();

        csv.Should().ContainAll(
            "Członek,Numer ewidencji,Chorągiew,Hufiec,Status,Certyfikat ważny do,Przydział",
            "Jan Kowalski,AA02,Chorągiew,Hufiec,ważny,2027-10-02,Drużyna Testowa",
            "Anna Nowak,AA03,Chorągiew,Hufiec,wygasł,2026-01-01,Drużyna Testowa",
            "Tomasz Bezcertyfikatu,AA04,Chorągiew,Hufiec,brak,,Drużyna Testowa");
    }
}
