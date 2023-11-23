using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmtpAccountCreationResultPublisher(IOptions<SmtpOptions> options, ISmtpClientFactory clientFactory) : IAccountCreationResultPublisher
{
    public async Task PublishResult(IReadOnlyCollection<AccountCreationResult> results, string requestorEmail)
    {
        if (results.Count == 0)
            return;

        var client = await clientFactory.GetClient();

        var recipientAdress = string.IsNullOrEmpty(options.Value.OverrideRecipient)
            ? requestorEmail
            : options.Value.OverrideRecipient;

        var html = BuildHtmlContent(results);

        var bodyBuilder = new BodyBuilder
        {
            TextBody = SmtpHelper.ClearHtml(html),
            HtmlBody = html
        };

        var mail = new MimeMessage(
            from: new[] { new MailboxAddress("Safe from Harm", options.Value.Username) },
            to: new[] { new MailboxAddress(recipientAdress, recipientAdress) },
            "Założone konta dla szkolenia Safe from Harm",
            bodyBuilder.ToMessageBody());

        await client.SendAsync(mail);
    }

    private static string BuildHtmlContent(IReadOnlyCollection<AccountCreationResult> results)
    {
        var builder = new StringBuilder("Czuwaj,<br>\n");

        var accountsCreated = results.Where(r => r.Result == AccountCreationResult.ResultType.Success).ToList();
        var errors = results.Where(r => r.Result != AccountCreationResult.ResultType.Success).ToList();

        if(accountsCreated.Count > 0)
        {
            builder.AppendLine("""
                Oto lista założonych na Twój wniosek kont Moodle na użytek szkoleń Safe from Harm:
                <ol>
                """);

            foreach (var account in accountsCreated)
                builder.AppendLine($"<li>{account.Member.FirstName} {account.Member.LastName} - Login: {account.Member.MembershipNumber}, Hasło: {account.Password}</li>");

            builder.AppendLine("</ol>");
        }

        if (errors.Count > 0)
        {
            builder.AppendLine("""
                Przy zakładaniu poniższych kont wystąpiły błędy:
                <ol>
                """);
            
            foreach (var error in errors)
                builder.AppendLine($"<li>{error.Member.FirstName} {error.Member.LastName} - {GetDescription(error.Result)}</li>");
            
            builder.AppendLine("""
                </ol>
                Aby uzyskać pomoc, skontaktuj się z chorągwianym pełnomocnikiem Safe from Harm.<br>
                """);
        }

        builder.AppendLine("""
            Z harcerskim pozdrowieniem,<br>
            Zespół Safe from Harm
            """);

        return builder.ToString();
    }

    private static string GetDescription(AccountCreationResult.ResultType result)
        => result switch
        {
            AccountCreationResult.ResultType.MemberNotInTipi => "Nie znaleziono aktywnego członka w Tipi",
            AccountCreationResult.ResultType.MemberHasMs365 => "Użytkownik ma konto Microsoft 365",
            AccountCreationResult.ResultType.MemberAlreadyHasMoodle => "Użytkownik ma już konto w Moodle",
            _ => $"Inny błąd ({result})",
        };
}
