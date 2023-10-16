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

        var bodyBuilder = new BodyBuilder
        {
            TextBody = BuildPlainTextContent(members),
            HtmlBody = BuildHtmlContent(members)
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", options.Username) },
            to: new[] { new MailboxAddress(supervisorUnitName, recipientAdress) },
            "Raport z niewykonanych szkoleń Safe from Harm",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }

    private static string BuildPlainTextContent(List<ZhpMember> missingCertificationMembers)
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

    private static string BuildHtmlContent(List<ZhpMember> missingCertificationMembers)
    {
        var b = new StringBuilder(
            """
            Czuwaj,<br>
            Oto lista członków ZHP z przydziałem do Twojej jednostki, którzy nie ukończyli obowiązkowego szkolenia z zakresu Polityki ochrony bezpieczeństwa dzieci, młodzieży i dorosłych, a tym samym nie posiadają ważnego certyfikatu "Safe From Harm":
            <ol>
            """);

        foreach (var member in missingCertificationMembers)
        {
            b.AppendLine($"<li>{member.FirstName} {member.LastName} ({member.MembershipNumber})</li>");
        }

        b.Append(
            """
            </ol>
            <p>Poproś ich o ukończenie e-szkolenia i wypełnienie testu w <a href="https://edu.zhp.pl/course/view.php?id=47">Harcerskim Serwisie Szkoleniowym</a>
            Jeśli ta informacja jest nieaktualna zaktualizuj Tipi, aby poprawić przydział i funkcje tych członków.
            Informacja o posiadaniu certyfikatu Safe from Harm powinna znaleźć się w Tipi w sekcji "Kursy, szkolenia i uprawnienia".</p>
            
            Posiadanie ważnego certyfikatu Safe From Harm, poświadczającego, że osoba ukończyła szkolenie z zakresu Polityki ochrony bezpieczeństwa dzieci, młodzieży i dorosłych w ZHP jest obowiązkiem:
            <ul>
            <li>wszystkich osób pełnoletnich w ZHP - w czasie 3 miesięcy od osiągnięcia pełnoletniości lub wstąpienia do organizacji,</li>
            <li>osób niepełnoletnich, pełniących funkcje wychowawcze - w momencie mianowania na funkcję,</li>
            <li>osób otwierających próby na stopnie instruktorskie.</li>
            </ul>

            <p>Wszystkie informacje o Polityce ochrony bezpieczeństwa dzieci, młodzieży i dorosłych w ZHP znajdziesz na stronie internetowej: <a href="https://zhp.pl/sfh">zhp.pl/sfh</a></p>
            Z harcerskim pozdrowieniem,<br>
            Zespół Safe from Harm
            """);

        return b.ToString();
    }
}
