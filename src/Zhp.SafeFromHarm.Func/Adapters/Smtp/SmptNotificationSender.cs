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

    public async Task NotifySupervisor(string supervisorEmail, string supervisorUnitName, IEnumerable<ZhpMember> missingCertificationMembers)
    {
        var members = missingCertificationMembers.ToList();
        if (!members.Any())
            return;

        var client = await clientFactory.GetClient();

        var recipientAdress = string.IsNullOrEmpty(options.OverrideRecipient)
            ? supervisorEmail
            : options.OverrideRecipient;

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress(options.Sender.DisplayName, options.Sender.Address) },
            to: new[] { new MailboxAddress(supervisorUnitName, recipientAdress) },
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
            Oto lista członków ZHP z przydziałem do Twojej jednostki, którzy nie ukończyli obowiązkowego szkolenia z zakresu Polityki ochrony bezpieczeństwa dzieci, młodzieży i dorosłych, a tym samym nie posiadają ważnego certyfikatu "Safe From Harm":

            """);

        foreach (var member in missingCertificationMembers)
        {
            b.AppendLine($"- {member.FirstName} {member.LastName} ({member.MembershipNumber})");
        }

        b.Append(
            """
            Poproś ich o ukończenie e-szkolenia i wypełnienie testu na https://edu.zhp.pl/course/view.php?id=47
            Jeśli ta informacja jest nieaktualna zaktualizuj Tipi, aby poprawić przydział i funkcje tych członków.
            Informacja o posiadaniu certyfikatu Safe from Harm powinna znaleźć się w Tipi w sekcji "Kursy, szkolenia i uprawnienia".
            
            Posiadanie ważnego certyfikatu Safe From Harm, poświadczającego, że osoba ukończyła szkolenie z zakresu Polityki ochrony bezpieczeństwa dzieci, młodzieży i dorosłych w ZHP jest obowiązkiem:
            - wszystkich osób pełnoletnich w ZHP - w czasie 3 miesięcy od osiągnięcia pełnoletniości lub wstąpienia do organizacji,
            - osób niepełnoletnich, pełniących funkcje wychowawcze - w momencie mianowania na funkcję,
            - osób otwierających próby na stopnie instruktorskie.
            
            Wszystkie informacje o Polityce ochrony bezpieczeństwa dzieci, młodzieży i dorosłych w ZHP znajdziesz na stronie internetowej: https://zhp.pl/sfh
            
            Z harcerskim pozdrowieniem,
            Zespół Safe from Harm
            """);

        return b.ToString();
    }
}
