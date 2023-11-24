using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Security.Cryptography;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Services;
using Zhp.SafeFromHarm.Func;
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

        var adapterToggles = ctx.Configuration.GetSection("Toggles").Get<AdapterTogglesOptions>();
        services
            .AddAccountCreation(adapterToggles)
            .AddAccountCreation(adapterToggles);
    })
    .ConfigureMoodleServices()
    .ConfigureSmtp()
    .ConfigureTipi()
    .Build();

host.Run();

file static class RegistrationExtensions {
    internal static IServiceCollection AddAccountCreation(this IServiceCollection services, AdapterTogglesOptions toggles)
    {
        services.AddTransient<AccountCreator>()
            .AddSingleton(s => RandomNumberGenerator.Create());

        services.AddSwitch("AccountCreator", toggles.AccountCreator, new()
        {
            ["Moodle"] = s => s.AddTransient<IAccountCreator, MoodleAccountCreator>(),
            ["Dummy"] = s => s.AddTransient<IAccountCreator, DummyAccountCreator>(),
        });

        services.AddSwitch("MemberMailAccountChecker", toggles.MemberMailAccountChecker, new()
        {
            ["Ms365"] = s => throw new NotImplementedException("TODO"),
            ["Dummy"] = s => s.AddTransient<IMemberMailAccountChecker, DummyMemberMailAccountChecker>(),
        });

        services.AddSwitch("MembersFetcher", toggles.MembersFetcher, new()
        {
            ["Tipi"] = s => s.AddTransient<IMembersFetcher, TipiMembersFetcher>(),
            ["Dummy"] = s => s.AddTransient<IMembersFetcher, DummyMembersFetcher>(),
        });

        foreach (var publisher in toggles.AccountCreationResultPublishers)
        {
            _ = publisher switch
            {
                "Dummy" => services.AddTransient<IAccountCreationResultPublisher, DummyAccountCreationResultPublisher>(),
                "Sharepoint" => throw new NotImplementedException("TODO"),
                "Smtp" => services.AddTransient<IAccountCreationResultPublisher, SmtpAccountCreationResultPublisher>(),
                _ => throw new Exception($"Unknown AccountCreationResultPublisher config value: {publisher ?? "null"}")
            };
        }
        return services;
    }

    internal static IServiceCollection ConfigureCertificationNotifications(this IServiceCollection services, AdapterTogglesOptions toggles)
    {
        services.AddTransient<MissingCertificationsNotifier>();

        services.AddSwitch("CertifiedMembersFetcher", toggles.CertifiedMembersFetcher, new()
        {
            ["Dummy"] = s => s.AddTransient<ICertifiedMembersFetcher, DummyCertifiedMembersFetcher>(),
            ["Moodle"] = s => s.AddTransient<ICertifiedMembersFetcher, MoodleCertifiedMembersFetcher>(),
        });

        services.AddSwitch("EmailMembershipNumberMapper", toggles.EmailMembershipNumberMapper, new()
        {
            ["Dummy"] = s => s.AddTransient<IEmailMembershipNumberMapper, DummyEmailMembershipNumberMapper>(),
            ["Moodle"] = s => s.AddSingleton<IEmailMembershipNumberMapper, MoodleEmailMembershipNumberMapper>(),
            ["Ms365"] = s => throw new NotImplementedException("Ms365 mail to membership number mapping not yet implemented"),
        });

        services.AddSwitch("RequiredMembersFetcher", toggles.RequiredMembersFetcher, new()
        {
            ["Dummy"] = s => s.AddTransient<IRequiredMembersFetcher, DummyRequiredMembersFetcher>(),
            ["Tipi"] = s => s.AddTransient<IRequiredMembersFetcher, TipiRequiredMembersFetcher>(),
        });

        services.AddSwitch("NotificationSender", toggles.NotificationSender, new()
        {
            ["Dummy"] = s => s
                        .AddTransient<INotificationSender, DummyNotificationSender>()
                        .AddTransient<ISummarySender, DummySummarySender>(),
            ["Smtp"] = s => s
                        .AddTransient<INotificationSender, SmtpNotificationSender>()
                        .AddTransient<ISummarySender, SmtpSummarySender>(),
        });

        return services;
    }

    private static IServiceCollection AddSwitch(this IServiceCollection services, string key, string? setting, Dictionary<string, Action<IServiceCollection>> registrations)
    {
        var action = registrations.GetValueOrDefault(setting ?? string.Empty, _ => throw new Exception($"Unknown {key} config value: {setting ?? "null"}"));
        action(services);

        return services;
    }
}
