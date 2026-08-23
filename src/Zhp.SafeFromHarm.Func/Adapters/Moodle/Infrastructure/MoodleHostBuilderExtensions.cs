using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;

internal static class MoodleHostBuilderExtensions
{
    private static readonly TimeSpan keepAliveInterval = TimeSpan.FromSeconds(30);
    private const int keepAliveRetryCount = 5;

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

                client.Timeout = TimeSpan.FromMinutes(10);

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
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { ConnectCallback = ConnectWithKeepAlive });
        });

        return builder;
    }

    /// <summary>
    /// Azure drops outbound connections idle for 4 minutes, and Moodle can chew on a single request for longer than that
    /// without sending a single byte. TCP keep-alive probes keep the flow alive so the load balancer doesn't cut it.
    /// </summary>
    private static async ValueTask<Stream> ConnectWithKeepAlive(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, (int)keepAliveInterval.TotalSeconds);
        socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, (int)keepAliveInterval.TotalSeconds);
        socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, keepAliveRetryCount);

        try
        {
            await socket.ConnectAsync(context.DnsEndPoint, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}
