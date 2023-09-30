using MailKit.Net.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal interface ISmtpClientFactory
{
    Task<ISmtpClient> GetClient();
}

internal class SmtpClientFactory : ISmtpClientFactory
{
    private readonly IServiceProvider provider;
    private readonly SmtpOptions options;
    private readonly Lazy<Task<ISmtpClient>> client;

    public SmtpClientFactory(IServiceProvider provider, IOptions<SmtpOptions> options)
    {
        this.provider = provider;
        this.options = options.Value;
        client = new(BuildClient);
    }

    public async Task<ISmtpClient> BuildClient()
    {
        var client = provider.GetRequiredService<ISmtpClient>();

        await client.ConnectAsync(options.Host, options.Port);
        await client.AuthenticateAsync(options.Username, options.Password);

        return client;
    }

    public Task<ISmtpClient> GetClient()
        => client.Value;
}
