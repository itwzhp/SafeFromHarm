using Microsoft.Extensions.Options;
using MimeKit;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmtpSummarySender : ISummarySender
{
    private readonly ISmtpClientFactory clientFactory;
    private readonly SmtpOptions smtpOptions;
    private readonly string teamsChannelMail;

    public SmtpSummarySender(ISmtpClientFactory clientFactory, IOptions<SmtpOptions> smtpOptions, IOptions<SafeFromHarmOptions> sfhOptions)
    {
        this.clientFactory = clientFactory;
        this.smtpOptions = smtpOptions.Value;
        this.teamsChannelMail = sfhOptions.Value.ControlTeamsChannelMail;
    }

    public async Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter)
    {
        var client = await clientFactory.GetClient();

        var sum = numberOfCertifedMembers + numberOfMissingCertificates;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $"""
                Zakończono wysyłkę maili do jednostek
                <ul>
                <li>Ukończonych szkoleń: {numberOfCertifedMembers} ({(double)numberOfCertifedMembers / sum:p})</li>
                <li>Nieukończonych szkoleń: {numberOfMissingCertificates} ({(double)numberOfMissingCertificates / sum:p})</li>
                <li>Razem: {sum}</li>
                </ul>
                {(mailFilter == null ? null : $"Mail wysłano <strong>tylko</strong> na adres " + mailFilter)}
                """,
            TextBody = "------"
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", smtpOptions.Username) },
            to: new[] { new MailboxAddress("SFH", teamsChannelMail) },
            "Podsumowanie wysyłki raportu",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }
}
