namespace Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;

internal class MoodleOptions
{
    public Uri MoodleBaseUri { get; set; } = new("https://edu.zhp.pl");

    public string MoodleToken { get; set; } = string.Empty;

    public int SfhCourseId { get; set; } = 47;
}
