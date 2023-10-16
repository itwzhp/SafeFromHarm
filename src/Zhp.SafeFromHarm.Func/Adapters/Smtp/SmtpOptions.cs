using System.Net.Mail;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmtpOptions
{
    public string Host { get; init; } = string.Empty;

    public MailAddress Sender => new(Username, "Safe from Harm");

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public ushort Port { get; init; }

    public string? OverrideRecipient { get; init; }
}
