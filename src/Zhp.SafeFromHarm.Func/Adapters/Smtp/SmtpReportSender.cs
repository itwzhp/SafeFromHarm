using Microsoft.Extensions.Options;
using MimeKit;
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
                """
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", options.Username) },
            to: new[] { new MailboxAddress(unit.Name, recipientMail) },
            "Raport chorągwiany ze szkoleń Safe from Harm",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }
}
