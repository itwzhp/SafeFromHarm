using MailKit.Net.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Zhp.SafeFromHarm.Func.Adapters.Smtp;

internal static class SmtpHostBuilderExtensions
{
    public static IHostBuilder ConfigureSmtp(this IHostBuilder builder)
            => builder.ConfigureServices(services =>
        {
            services.AddOptions<SmtpOptions>()
                .BindConfiguration("Smtp")
                .Validate(opt => !string.IsNullOrWhiteSpace(opt.Host))
                .Validate(opt => !string.IsNullOrWhiteSpace(opt.Username))
                .Validate(opt => opt.Port > 0);

            services.AddTransient<ISmtpClient, SmtpClient>();
            services.AddTransient<ISmtpClientFactory, SmtpClientFactory>();
        });
}
