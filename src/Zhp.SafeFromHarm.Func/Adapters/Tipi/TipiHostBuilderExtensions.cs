using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;

namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

internal static class TipiHostBuilderExtensions
{
    public static IServiceCollection AddTipiRequiredMembersFetcher(this IServiceCollection services)
        => services.AddTipiWithHttpClient<IRequiredMembersFetcher, TipiRequiredMembersFetcher>()
            .AddTipiWithHttpClient<TipiUnitsFetcher>();

    public static IServiceCollection AddTipiMembersFetcher(this IServiceCollection services)
        => services.AddTipiWithHttpClient<IMembersFetcher, TipiMembersFetcher>();

    private static IServiceCollection AddTipiWithHttpClient<TClient, TImplementation>(this IServiceCollection services)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddOptions<TipiOptions>()
            .BindConfiguration("Tipi")
            .Validate(opt => !string.IsNullOrWhiteSpace(opt.TokenSecret))
            .Validate(opt => !string.IsNullOrWhiteSpace(opt.TokenId));

        services.AddHttpClient<TClient, TImplementation>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<TipiOptions>>().Value;

            client.BaseAddress = options.BaseUrl;
            client.DefaultRequestHeaders.Add("CF-Access-Client-Id", options.TokenId);
            client.DefaultRequestHeaders.Add("CF-Access-Client-Secret", options.TokenSecret);
        })
            .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(5, i => TimeSpan.FromSeconds(2)));

        return services;
    }

    private static IServiceCollection AddTipiWithHttpClient<TImplementation>(this IServiceCollection services)
        where TImplementation : class
        => services.AddTipiWithHttpClient<TImplementation, TImplementation>();
}
