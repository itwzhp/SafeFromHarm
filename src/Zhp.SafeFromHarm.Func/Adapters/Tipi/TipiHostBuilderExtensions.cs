using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Zhp.SafeFromHarm.Domain.Ports;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal static class TipiHostBuilderExtensions
{
    public static IHostBuilder ConfigureTipi(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddOptions<TipiOptions>()
                .BindConfiguration("Tipi")
                .Validate(opt => !string.IsNullOrWhiteSpace(opt.TokenSecret))
                .Validate(opt => !string.IsNullOrWhiteSpace(opt.TokenId));

            services.AddHttpClient<IRequiredMembersFetcher, TipiRequiredMembersFetcher>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<TipiOptions>>().Value;

                client.BaseAddress = options.BaseUrl;
                client.DefaultRequestHeaders.Add("CF-Access-Client-Id", options.TokenId);
                client.DefaultRequestHeaders.Add("CF-Access-Client-Secret", options.TokenSecret);
            })
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(2)));
        });
        return builder;
    }
}
