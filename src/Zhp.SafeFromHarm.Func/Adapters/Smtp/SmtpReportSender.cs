using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmtpReportSender(
    ISmtpClientFactory clientFactory,
    IOptions<SmtpOptions> options,
    IUnitContactMailProvider mailProvider) : IReportSender
{
    private readonly SmtpOptions options = options.Value;
    public async Task SendReport(Unit unit, IEnumerable<CertificationReport.ReportEntry> entries)
    {
        var entriesList = entries.ToList();

        var client = await clientFactory.GetClient();

        var recipients = !string.IsNullOrEmpty(options.OverrideRecipient)
            ? [new MailboxAddress(unit.Name, options.OverrideRecipient)]
            : await mailProvider.GetEmailAddresses(unit.Id).Append(unit.Email).Distinct()
                .Select(m => new MailboxAddress(unit.Name, m))
                .ToListAsync();

        var html = BuildRegionalHtml(entriesList);
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = html,
            TextBody = SmtpHelper.ClearHtml(html),
            Attachments =
            {
                new MimePart(System.Net.Mime.MediaTypeNames.Text.Csv)
                {
                    Content = new MimeContent(BuildReportAttachment(entriesList)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = $"Raport SFH-{DateTime.Today:yyyy-MM-dd}-{unit.Name}.csv"
                }
            }
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", options.Username) },
            to: recipients,
            $"Raport chorągwiany ze szkoleń Safe from Harm - {unit.Name}",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }

    private static MemoryStream BuildReportAttachment(IReadOnlyCollection<CertificationReport.ReportEntry> entries)
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(true), leaveOpen: true);

        writer.WriteLine("Członek,Numer ewidencji,Chorągiew,Hufiec,Data certyfikacji");
        foreach (var entry in entries)
        {
            writer.WriteLine($"{entry.Member.FirstName} {entry.Member.LastName},{entry.Member.MembershipNumber},{entry.Member.Department.Name},{entry.Member.Supervisor.Name},{entry.CertificationDate}");
        }

        return stream;
    }

    private static string BuildRegionalHtml(IReadOnlyCollection<CertificationReport.ReportEntry> entries)
    {
        var totalCertified = entries.Count(e => e.CertificationDate != null);
        var total = entries.Count;
        var totalPercentage = (double)totalCertified / total;

        var builder = new StringBuilder($"""
            Czuwaj,<br>
            <p>Oto raport z wykonywania szkoleń Safe from Harm dla Twojej chorągwi. Raporty zostały również przesłane poszczególnym hufcom.</p>
            <p>W Chorągwi certyfikowano {totalCertified} z {total} wymaganych ({totalPercentage:P0}), w poszczególnych hufcach jest to:
            <ul>
            """);

        foreach (var entry in entries.GroupBy(e => e.Member.Supervisor))
        {
            var entriesForUnit = entry.ToList();
            var certified = entriesForUnit.Count(e => e.CertificationDate != null);
            var totalForUnit = entriesForUnit.Count;
            var percentage = (double)certified / totalForUnit;
            builder.AppendLine($"<li>{entry.Key.Name} ({certified}/{totalForUnit}, {percentage:P0})</li>");
        }

        builder.AppendLine("""
            </ul></p>
            Z harcerskim pozdrowieniem,<br>
            Zespół Safe from Harm
            """);

        return builder.ToString();
    }

    public async Task SendCentralReport(CertificationReport report)
    {
        const string centralRecipient = "Pełnomocnik SFH";
        const int centralUnitId = 2;

        var client = await clientFactory.GetClient();

        var recipients = !string.IsNullOrEmpty(options.OverrideRecipient)
            ? [new MailboxAddress(centralRecipient, options.OverrideRecipient)]
            : await mailProvider.GetEmailAddresses(centralUnitId).Distinct()
                .Select(m => new MailboxAddress(centralRecipient, m))
                .ToListAsync();

        var html = BuildCentralHtml(report);
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = html,
            TextBody = SmtpHelper.ClearHtml(html)
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", options.Username) },
            to: recipients,
            $"Raport centralny ze szkoleń Safe from Harm",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }

    private static string BuildCentralHtml(CertificationReport report)
    {
        var totalPercentage = (double)report.NumberCertified / report.NumberToCertify;

        var builder = new StringBuilder($"""
            Czuwaj,<br>
            <p>Oto raport z wykonywania szkoleń Safe from Harm dla ZHP.</p>
            <p>W Certyfikowano {report.NumberCertified} z {report.NumberToCertify} wymaganych ({totalPercentage:P0}), w poszczególnych jednostkach jest to:
            <ul>
            """);

        foreach (var department in report.Entries.GroupBy(e => e.Member.Department))
        {
            var entriesForDepartment = department.ToList();
            var certified = entriesForDepartment.Count(e => e.CertificationDate != null);
            var totalForDepartment = entriesForDepartment.Count;
            var percentage = (double)certified / totalForDepartment;

            builder.AppendLine($"<li>{department.Key.Name} ({certified}/{totalForDepartment}, {percentage:P0}), w tym:<ul>");

            foreach (var unit in entriesForDepartment.GroupBy(e => e.Member.Supervisor))
            {
                var entriesForUnit = unit.ToList();
                var certifiedForUnit = entriesForUnit.Count(e => e.CertificationDate != null);
                var totalForUnit = entriesForUnit.Count;
                var percentageForUnit = (double)certifiedForUnit / totalForUnit;

                builder.AppendLine($"<li>{unit.Key.Name} ({certifiedForUnit}/{totalForUnit}, {percentageForUnit:P0}),</li>");
            }

            builder.AppendLine("</ul></li>");
        }

        builder.AppendLine("""
            </ul></p>
            Z harcerskim pozdrowieniem,<br>
            Zespół Safe from Harm
            """);

        return builder.ToString();
    }
}
