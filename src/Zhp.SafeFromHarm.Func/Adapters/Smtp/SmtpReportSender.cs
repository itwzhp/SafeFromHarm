using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmtpReportSender(
    ISmtpClientFactory clientFactory,
    IOptions<SmtpOptions> options) : IReportSender
{
    private readonly SmtpOptions options = options.Value;
    public async Task SendReport(Unit unit, IEnumerable<CertificationReport.ReportEntry> entries)
    {
        var recipientMail = string.IsNullOrEmpty(options.OverrideRecipient)
            ? unit.Email
            : options.OverrideRecipient;

        var client = await clientFactory.GetClient();

        var bodyBuilder = new BodyBuilder
        {
            TextBody = """
                Czuwaj,
                Oto raport z wykonywania szkoleń Safe from Harm dla Twojej chorągwi. Raporty zostały również przesłane poszczególnym hufcom.
                Z harcerskim pozdrowieniem,
                Zespół Safe from Harm
                """,
            Attachments =
            {
                new MimePart(System.Net.Mime.MediaTypeNames.Text.Csv)
                {
                    Content = new MimeContent(BuildReport(entries)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = $"Raport SFH-{DateTime.Today:yyyy-MM-dd}-{unit.Name}.csv"
                }
            }
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", options.Username) },
            to: new[] { new MailboxAddress(unit.Name, recipientMail) },
            $"Raport chorągwiany ze szkoleń Safe from Harm - {unit.Name}",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }

    private static MemoryStream BuildReport(IEnumerable<CertificationReport.ReportEntry> entries)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true);

        writer.WriteLine("Członek;Numer ewidencji;Hufiec;Data certyfikacji");
        foreach (var entry in entries)
        {
            writer.WriteLine($"{entry.Member.FirstName} {entry.Member.LastName};{entry.Member.MembershipNumber};{entry.Member.Supervisor.Name};{entry.CertificationDate}");
        }

        return stream;
    }
}
