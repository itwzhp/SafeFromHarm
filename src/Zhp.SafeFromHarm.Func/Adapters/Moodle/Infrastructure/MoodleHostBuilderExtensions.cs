﻿using Microsoft.Extensions.DependencyInjection;
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

                client.BaseAddress = options.MoodleBaseUri;
                client.Timeout = TimeSpan.FromMinutes(5);
            });
        });

        return builder;
    }
}
