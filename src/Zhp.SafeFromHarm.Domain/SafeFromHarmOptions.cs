namespace Zhp.SafeFromHarm.Domain;

public class SafeFromHarmOptions
{
    public string? ControlTeamsChannelMail { get; init; }

    public string MoodleAccountMailFakeDomain { get; init; } = "sfh.fake-mail.zhp.pl";
}
