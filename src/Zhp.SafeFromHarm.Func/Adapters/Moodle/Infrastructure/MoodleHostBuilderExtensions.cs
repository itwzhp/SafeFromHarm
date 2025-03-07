using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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

            services.AddHttpClient<MoodleClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<MoodleOptions>>().Value;

                client.Timeout = TimeSpan.FromMinutes(5);

                if (string.IsNullOrEmpty(options.MoodleHostName))
                {
                    client.BaseAddress = options.MoodleBaseUri;
                }
                else
                {
                    // This is a workaround for timeout built in CloudFlare. This way we can bypass it. and make request longer than 100 seconds.
                    client.BaseAddress = new UriBuilder(options.MoodleBaseUri) { Host = options.MoodleHostName }.Uri;
                    client.DefaultRequestHeaders.Host = options.MoodleBaseUri.Host;
                }
            });
        });

        return builder;
    }
}
