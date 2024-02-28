using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;

namespace Zhp.SafeFromHarm.Tests.Adapters.Moodle;

internal static class MoodleTestFactory
{
    public static MoodleClient MoodleClient => new(new HttpClient(new MoodleTestHttpHandler()) { BaseAddress = new("https://example.zhp.pl")}, MoodleOptions);

    public static IOptions<MoodleOptions> MoodleOptions => new OptionsWrapper<MoodleOptions>(new());
}
