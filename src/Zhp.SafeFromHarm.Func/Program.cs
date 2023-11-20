using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Security.Cryptography;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Services;
using Zhp.SafeFromHarm.Func.Adapters.Moodle;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;
using Zhp.SafeFromHarm.Func.Adapters.Smtp;
using Zhp.SafeFromHarm.Func.Adapters.TestDummy;
using Zhp.SafeFromHarm.Func.Adapters.Tipi;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((ctx, config)
        => config
            .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
            .AddUserSecrets(Assembly.GetExecutingAssembly()))
    .ConfigureServices((ctx, services) =>
    {
        services.AddOptions<SafeFromHarmOptions>()
            .BindConfiguration("SafeFromHarm")
            .Validate(sfh => sfh.CertificateExpiryDays > 0);
    })
    .ConfigureMoodleServices()
    .ConfigureSmtp()
    .ConfigureTipi()
    .ConfigureAccountCreation()
    .ConfigureCertificationNotifications()
    .Build();

host.Run();

static class RegistrationExtensions {
    internal static IHostBuilder ConfigureAccountCreation(this IHostBuilder builder)
        => builder.ConfigureServices((ctx, services) =>
            {
                services.AddTransient<AccountCreator>()
                    .AddSingleton(s => RandomNumberGenerator.Create());

                services.AddSwitch("AccountCreator", ctx, new()
                {
                    ["Moodle"] = s => throw new NotImplementedException("TODO"),
                    ["Dummy"] = s => s.AddTransient<IAccountCreator, DummyAccountCreator>(),
                });

                services.AddSwitch("AccountCreationResultPublisher", ctx, new()
                {
                    ["Sharepoint"] = s => throw new NotImplementedException("TODO"),
                    ["Dummy"] = s => s.AddTransient<IAccountCreationResultPublisher, DummyAccountCreationResultPublisher>(),
                });

                services.AddSwitch("MemberMailAccountChecker", ctx, new()
                {
                    ["Ms365"] = s => throw new NotImplementedException("TODO"),
                    ["Dummy"] = s => s.AddTransient<IMemberMailAccountChecker, DummyMemberMailAccountChecker>(),
                });

                services.AddSwitch("MembersFetcher", ctx, new()
                {
                    ["Tipi"] = s => throw new NotImplementedException("TODO"),
                    ["Dummy"] = s => s.AddTransient<IMembersFetcher, DummyMembersFetcher>(),
                });
            });

    internal static IHostBuilder ConfigureCertificationNotifications(this IHostBuilder builder)
        => builder.ConfigureServices((ctx, services) =>
            {
                services.AddTransient<MissingCertificationsNotifier>();

                services.AddSwitch("CertifiedMembersFetcher", ctx, new()
                {
                    ["Dummy"] = s => s.AddTransient<ICertifiedMembersFetcher, DummyCertifiedMembersFetcher>(),
                    ["Moodle"] = s => s.AddTransient<ICertifiedMembersFetcher, MoodleCertifiedMembersFetcher>(),
                });

                services.AddSwitch("EmailMembershipNumberMapper", ctx, new()
                {
                    ["Dummy"] = s => s.AddTransient<IEmailMembershipNumberMapper, DummyEmailMembershipNumberMapper>(),
                    ["Moodle"] = s => s.AddSingleton<IEmailMembershipNumberMapper, MoodleEmailMembershipNumberMapper>(),
                    ["Ms365"] = s => throw new NotImplementedException("Ms365 mail to membership number mapping not yet implemented"),
                });

                services.AddSwitch("RequiredMembersFetcher", ctx, new()
                {
                    ["Dummy"] = s => s.AddTransient<IRequiredMembersFetcher, DummyRequiredMembersFetcher>(),
                    ["Tipi"] = s => s.AddTransient<IRequiredMembersFetcher, TipiRequiredMembersFetcher>(),
                });

                services.AddSwitch("NotificationSender", ctx, new()
                {
                    ["Dummy"] = s => s
                                .AddTransient<INotificationSender, DummyNotificationSender>()
                                .AddTransient<ISummarySender, DummySummarySender>(),
                    ["Smtp"] = s => s
                                .AddTransient<INotificationSender, SmtpNotificationSender>()
                                .AddTransient<ISummarySender, SmtpSummarySender>(),
                });
            });

    private static IServiceCollection AddSwitch(this IServiceCollection services, string key, HostBuilderContext ctx, Dictionary<string, Action<IServiceCollection>> registrations)
    {
        var setting = ctx.Configuration[key] ?? string.Empty;

        var action = registrations.GetValueOrDefault(setting, _ => throw new Exception($"Unknown {key} config value: {setting ?? "null"}"));
        action(services);

        return services;
    }
}
