using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;

namespace Zhp.SafeFromHarm.Func.Adapters.GraphApi;

internal static class GraphApiHostBuilderExtensions
{
    private static readonly string[] scopes = ["https://graph.microsoft.com/.default"];

    public static IHostBuilder ConfigureGraphApi(this IHostBuilder builder)
        => builder.ConfigureServices((ctx, services) =>
        {
            services.AddOptions<GraphApiOptions>()
                .BindConfiguration("GraphApi")
                .Validate(o => o.SfhSiteId != Guid.Empty)
                .Validate(o => o.CreatedAccountsListId != Guid.Empty);

            if (ctx.HostingEnvironment.IsDevelopment())
                services.AddSingleton<TokenCredential, InteractiveBrowserCredential>();
            else
                services.AddSingleton<TokenCredential, ManagedIdentityCredential>();

            services.AddSingleton(sp => new GraphServiceClient(sp.GetRequiredService<TokenCredential>(), scopes));
        });
}
