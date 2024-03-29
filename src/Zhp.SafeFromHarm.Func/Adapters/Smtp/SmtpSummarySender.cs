﻿using Microsoft.Extensions.Options;
using MimeKit;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmtpSummarySender(ISmtpClientFactory clientFactory, IOptions<SmtpOptions> smtpOptions, IOptions<SafeFromHarmOptions> sfhOptions) : ISummarySender
{
    private readonly SmtpOptions smtpOptions = smtpOptions.Value;
    private readonly string? teamsChannelMail = sfhOptions.Value.ControlTeamsChannelMail;

    public async Task SendCentralReport(CertificationReport report, string? mailFilter, IReadOnlyCollection<Unit> failedRecipients)
    {
        if (teamsChannelMail == null)
            return;

        var client = await clientFactory.GetClient();

        var body = $"""
            Zakończono wysyłkę maili do jednostek
            <ul>
            <li>Ukończonych szkoleń: {report.NumberCertified} ({(double)report.NumberCertified / report.NumberToCertify:p})</li>
            <li>Nieukończonych szkoleń: {report.NumberNotCertified} ({(double)report.NumberNotCertified / report.NumberToCertify:p})</li>
            <li>Razem: {report.NumberToCertify}</li>
            </ul>
            {(mailFilter == null ? null : $"Mail wysłano <strong>tylko</strong> na adres " + mailFilter)}
            """;

        if (failedRecipients.Count != 0)
        {
            body += "<br>Nie udało się wysłać maila do poniższych jednostek:<ol>";
            foreach (var unit in failedRecipients)
                body += $"<li>{unit}</li>";
            body += "</ol>";
        }

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body,
            TextBody = "------"
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", smtpOptions.Username) },
            to: new[] { new MailboxAddress("SFH", teamsChannelMail) },
            "Podsumowanie wysyłki raportu dla chorągwi",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }

    public async Task SendSummary(int numberOfCertifedMembers, int numberOfMissingCertificates, string? mailFilter, IReadOnlyCollection<(string Email, string UnitName)> failedRecipients)
    {
        if (teamsChannelMail == null)
            return;

        var client = await clientFactory.GetClient();

        var sum = numberOfCertifedMembers + numberOfMissingCertificates;

        var body = $"""
                Zakończono wysyłkę maili do jednostek
                <ul>
                <li>Ukończonych szkoleń: {numberOfCertifedMembers} ({(double)numberOfCertifedMembers / sum:p})</li>
                <li>Nieukończonych szkoleń: {numberOfMissingCertificates} ({(double)numberOfMissingCertificates / sum:p})</li>
                <li>Razem: {sum}</li>
                </ul>
                {(mailFilter == null ? null : $"Mail wysłano <strong>tylko</strong> na adres " + mailFilter)}
                """;
        if (failedRecipients.Count != 0)
        {
            body += "<br>Nie udało się wysłać maila do poniższych jednostek:<ol>";
            foreach (var (Email, UnitName) in failedRecipients)
                body += $"<li>{UnitName} ({Email})</li>";
            body += "</ol>";
        }

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body,
            TextBody = "------"
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", smtpOptions.Username) },
            to: new[] { new MailboxAddress("SFH", teamsChannelMail) },
            "Podsumowanie wysyłki raportu dla jednostek",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }
}
