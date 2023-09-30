using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmptNotificationSender : INotificationSender
{
    private readonly SmtpOptions options;
    private readonly ISmtpClientFactory clientFactory;

    public SmptNotificationSender(IOptions<SmtpOptions> options, ISmtpClientFactory clientFactory)
    {
        this.options = options.Value;
        this.clientFactory = clientFactory;
    }

    public async Task NotifySupervisor(string supervisorEmail, IEnumerable<ZhpMember> missingCertificationMembers)
    {
        var members = missingCertificationMembers.ToList();
        if (!members.Any())
            return;

        var client = await clientFactory.GetClient();

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress(options.Sender.DisplayName, options.Sender.Address) },
            to: new[] { new MailboxAddress(members.First().SupervisorUnitName, supervisorEmail) },
            "Raport z niewykonanych szkoleń Safe from Harm",
            new TextPart("plain")
            {
                Text = BuildContent(members)
            });

        await client.SendAsync(mail);
    }

    private static string BuildContent(List<ZhpMember> missingCertificationMembers)
    {
        var b = new StringBuilder(
            """
            Czuwaj,
            Oto lista członków ZHP z przydziałem do Twojej jednostki, którzy nie ukończyli obowiązkowego szkolenia Safe From Harm:

            """);

        foreach (var member in missingCertificationMembers)
        {
            b.AppendLine($"- {member.FirstName} {member.LastName} ({member.MembershipNumber})");
        }

        b.Append(
            """
            Poproś ich o ukończenie testu na https://edu.zhp.pl/course/view.php?id=47.
            Jeśli ta informacja jest nieaktualna zaktualizuj Tipi, aby poprawić przydział i funkcje tych członków
            Z harcerskim pozdrowieniem,
            Zespół Safe from Harm
            """);

        return b.ToString();
    }
}
