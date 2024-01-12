using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmtpNotificationSender(
    IOptions<SmtpOptions> options,
    ISmtpClientFactory clientFactory,
    IUnitContactMailProvider mailProvider) : INotificationSender
{
    private readonly SmtpOptions options = options.Value;

    public async Task NotifySupervisor(Unit supervisor, IEnumerable<MemberToCertify> missingCertificationMembers, IEnumerable<(MemberToCertify Member, DateOnly CertificationDate)> certifiedMembers)
    {
        var membersToCertify = missingCertificationMembers.ToList();
        var certifiedMemebersList = certifiedMembers.ToList();
        if (membersToCertify.Count == 0 && certifiedMemebersList.Count == 0)
            return;

        var client = await clientFactory.GetClient();

        var recipients = !string.IsNullOrEmpty(options.OverrideRecipient)
            ? [ new MailboxAddress(supervisor.Name, options.OverrideRecipient) ]
            : await mailProvider.GetEmailAddresses(supervisor.Id).Append(supervisor.Email).Distinct()
                .Select(m => new MailboxAddress(supervisor.Name, m))
                .ToListAsync();

        var html = BuildHtmlContent(membersToCertify, certifiedMemebersList);

        var bodyBuilder = new BodyBuilder
        {
            TextBody = SmtpHelper.ClearHtml(html),
            HtmlBody = html
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", options.Username) },
            to: recipients,
            "Raport z niewykonanych szkoleń Safe from Harm",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }

    private static string BuildHtmlContent(List<MemberToCertify> missingCertificationMembers, List<(MemberToCertify Member, DateOnly CertificationDate)> certifiedMembers)
    {
        var b = new StringBuilder("Czuwaj,<br>\n");

        if (missingCertificationMembers.Count != 0)
        {
            b.AppendLine("""
                Oto lista członków ZHP z przydziałem do Twojej jednostki, którzy <strong>nie ukończyli</strong> obowiązkowego szkolenia z zakresu Polityki ochrony bezpieczeństwa dzieci, młodzieży i dorosłych, a tym samym nie posiadają ważnego certyfikatu "Safe From Harm":
                <ol>
                """);

            foreach (var member in missingCertificationMembers)
                b.AppendLine($"<li>{member.FirstName} {member.LastName} ({member.MembershipNumber})</li>");

            b.AppendLine("""
                    </ol>
                    <p>Poproś ich o ukończenie e-szkolenia i wypełnienie testu w <a href="https://edu.zhp.pl/course/view.php?id=47">Harcerskim Serwisie Szkoleniowym</a>.
                    Jeśli ta informacja jest nieaktualna zaktualizuj Tipi, aby poprawić przydział i funkcje tych członków.</p>
                    """);
        }

        if(certifiedMembers.Count != 0)
        {
            b.AppendLine("""
                Poniżej znajduje się lista członków ZHP z przydziałem do Twojej jednostki, którzy <strong>ukończyli</strong> obowiązkowe szkolenie:
                <ol>
                """);

            foreach (var (member, certificationDate) in certifiedMembers)
                b.AppendLine($"<li>{member.FirstName} {member.LastName} ({member.MembershipNumber}) - {certificationDate:dd.MM.yyyy}</li>");
            
            b.AppendLine("""
               </ol>
               Informacja o posiadaniu certyfikatu Safe from Harm powinna znaleźć się w Tipi w sekcji "Kursy, szkolenia i uprawnienia".
               """);
        }

        b.Append(
            """
            <br>            
            Posiadanie ważnego certyfikatu Safe From Harm, poświadczającego, że osoba ukończyła szkolenie z zakresu Polityki ochrony bezpieczeństwa dzieci, młodzieży i dorosłych w ZHP jest obowiązkiem:
            <ul>
            <li>wszystkich osób pełnoletnich w ZHP - w czasie 3 miesięcy od osiągnięcia pełnoletniości lub wstąpienia do organizacji,</li>
            <li>osób niepełnoletnich, pełniących funkcje wychowawcze - w momencie mianowania na funkcję,</li>
            <li>osób otwierających próby na stopnie instruktorskie.</li>
            </ul>
            Certyfikat jest ważny przez trzy lata od jego wydania.

            <p>Wszystkie informacje o Polityce ochrony bezpieczeństwa dzieci, młodzieży i dorosłych w ZHP znajdziesz na stronie internetowej: <a href="https://zhp.pl/sfh">zhp.pl/sfh</a></p>
            Z harcerskim pozdrowieniem,<br>
            Zespół Safe from Harm
            """);

        return b.ToString();
    }
}
