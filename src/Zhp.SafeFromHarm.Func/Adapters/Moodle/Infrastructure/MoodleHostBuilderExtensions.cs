using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;

internal static class MoodleHostBuilderExtensions
{
    public static IHostBuilder ConfigureMoodleServices(this IHostBuilder builder)
    {
        builder.ConfigureServices((ctx, services) =>
        {
            services.AddOptions<MoodleOptions>()
                .BindConfiguration("Moodle")
                .Validate(opt => !string.IsNullOrWhiteSpace(opt.MoodleToken));

            services.AddSingleton<MoodleClient>();
        });

        return builder;
    }
}
