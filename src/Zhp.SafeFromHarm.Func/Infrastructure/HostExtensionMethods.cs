using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Security.Cryptography;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Helpers;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;
using Zhp.SafeFromHarm.Domain.Ports.CertificationNotifications;
using Zhp.SafeFromHarm.Domain.Services;
using Zhp.SafeFromHarm.Func.Adapters.GraphApi;
using Zhp.SafeFromHarm.Func.Adapters.Moodle;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;
using Zhp.SafeFromHarm.Func.Adapters.Smtp;
using Zhp.SafeFromHarm.Func.Adapters.TestDummy;
using Zhp.SafeFromHarm.Func.Adapters.Tipi;
using Zhp.SafeFromHarm.Func.Functions;

namespace Zhp.SafeFromHarm.Func.Infrastructure;

public static class HostExtensionMethods
{
    public static IHostBuilder ConfigureSafeFromHarmHost(this IHostBuilder builder)
    {
        return builder
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureAppConfiguration((ctx, config)
                => config
                    .AddJsonFile($"appsettings.json", optional: false, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false)
                    .AddUserSecrets(Assembly.GetExecutingAssembly()))
            .ConfigureServices((ctx, services) =>
            {
                services.AddOptions<SafeFromHarmOptions>()
                    .BindConfiguration("SafeFromHarm")
                    .Validate(sfh => sfh.CertificateExpiryDays > 0);

                var adapterToggles = ctx.Configuration.GetSection("Toggles").Get<AdapterTogglesOptions>() ?? new();
                services
                    .AddAccountCreation(adapterToggles)
                    .AddCertificationNotifications(adapterToggles);
            })
            .ConfigureMoodleServices()
            .ConfigureSmtp()
            .ConfigureGraphApi();
    }

    internal static IHost AssertFunctionRegistrations(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<CreateAccounts>();
        _ = scope.ServiceProvider.GetRequiredService<FindMissingRequiredCertifications>();
        _ = scope.ServiceProvider.GetRequiredService<GenerateReports>();
        return host;
    }

    private static IServiceCollection AddAccountCreation(this IServiceCollection services, AdapterTogglesOptions toggles)
    {
        services.AddTransient<AccountCreator>()
            .AddTransient<PasswordGenerator>()
            .AddTransient<CreateAccounts>()
            .AddSingleton(s => RandomNumberGenerator.Create());

        services.AddSwitch("AccountCreator", toggles.AccountCreator, new()
        {
            ["Moodle"] = s => s.AddTransient<IAccountCreator, MoodleAccountCreator>(),
            ["Dummy"] = s => s.AddTransient<IAccountCreator, DummyAccountCreator>(),
        });

        services.AddSwitch("MemberMailAccountChecker", toggles.MemberMailAccountChecker, new()
        {
            ["Ms365"] = s => s.AddTransient<IMemberMailAccountChecker, EntraIdMemberMailAccountChecker>(),
            ["Dummy"] = s => s.AddTransient<IMemberMailAccountChecker, DummyMemberMailAccountChecker>(),
        });

        services.AddSwitch("MembersFetcher", toggles.MembersFetcher, new()
        {
            ["Tipi"] = s => s.AddTipiMembersFetcher(),
            ["Dummy"] = s => s.AddTransient<IMembersFetcher, DummyMembersFetcher>(),
        });

        foreach (var publisher in toggles.AccountCreationResultPublishers)
        {
            _ = publisher switch
            {
                "Dummy" => services.AddTransient<IAccountCreationResultPublisher, DummyAccountCreationResultPublisher>(),
                "Sharepoint" => services.AddTransient<IAccountCreationResultPublisher, SharepointAccountCreationResultPublisher>(),
                "Smtp" => services.AddTransient<IAccountCreationResultPublisher, SmtpAccountCreationResultPublisher>(),
                _ => throw new Exception($"Unknown AccountCreationResultPublisher config value: {publisher ?? "null"}")
            };
        }
        return services;
    }

    private static IServiceCollection AddCertificationNotifications(this IServiceCollection services, AdapterTogglesOptions toggles)
    {
        services
            .AddTransient<CertificationReportProvider>()
            .AddTransient<FindMissingRequiredCertifications>()
            .AddTransient<MissingCertificationsNotifier>()
            .AddTransient<ReportGenerator>()
            .AddTransient<GenerateReports>();

        services.AddSwitch("CertifiedMembersFetcher", toggles.CertifiedMembersFetcher, new()
        {
            ["Dummy"] = s => s.AddTransient<ICertifiedMembersFetcher, DummyCertifiedMembersFetcher>(),
            ["Moodle"] = s => s.AddTransient<ICertifiedMembersFetcher, MoodleCertifiedMembersFetcher>(),
        });

        services.AddSwitch("EmailMembershipNumberMapper", toggles.EmailMembershipNumberMapper, new()
        {
            ["Dummy"] = s => s.AddTransient<IEmailMembershipNumberMapper, DummyEmailMembershipNumberMapper>(),
            ["Moodle"] = s => s.AddSingleton<IEmailMembershipNumberMapper, MoodleEmailMembershipNumberMapper>(),
            ["Ms365"] = s => s.AddSingleton<IEmailMembershipNumberMapper, EntraIdEmailMembershipNumberMapper>(),
        });

        services.AddSwitch("RequiredMembersFetcher", toggles.RequiredMembersFetcher, new()
        {
            ["Dummy"] = s => s.AddTransient<IRequiredMembersFetcher, DummyRequiredMembersFetcher>(),
            ["Tipi"] = s => s.AddTipiRequiredMembersFetcher(),
        });

        services.AddSwitch("NotificationSender", toggles.NotificationSender, new()
        {
            ["Dummy"] = s => s
                        .AddTransient<INotificationSender, DummyNotificationSender>()
                        .AddTransient<ISummarySender, DummySummarySender>()
                        .AddTransient<IReportSender, DummyReportSender>(),
            ["Smtp"] = s => s
                        .AddTransient<INotificationSender, SmtpNotificationSender>()
                        .AddTransient<ISummarySender, SmtpSummarySender>()
                        .AddTransient<IReportSender, SmtpReportSender>(),
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