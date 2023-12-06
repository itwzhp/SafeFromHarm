namespace Zhp.SafeFromHarm.Domain;

public class SafeFromHarmOptions
{
    public int CertificateExpiryDays { get; init; } = 365 * 3 + 1;

    public string? ControlTeamsChannelMail { get; init; }

    public string MoodleAccountMailFakeDomain { get; init; } = "sfh.fake-mail.zhp.pl";
}
