using System.Net.Mail;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public MailAddress Sender { get; set; } = new("safe.from.harm@mail-auto.zhp.pl", "Safe From Harm");

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public ushort Port { get; set; }
}
