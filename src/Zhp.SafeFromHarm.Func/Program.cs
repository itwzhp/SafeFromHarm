using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Ports;
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
            .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true)
            .AddUserSecrets(Assembly.GetExecutingAssembly()))
    .ConfigureServices((ctx, services) =>
    {
        _ = ctx.Configuration["CertifiedMembersFetcher"] switch
        {
            "Dummy" => services.AddTransient<ICertifiedMembersFetcher, DummyCertifiedMembersFetcher>(),
            "Moodle" => services.AddTransient<ICertifiedMembersFetcher, MoodleCertifiedMembersFetcher>(),
            _ => throw new Exception($"Unknown CertifiedMembersFetcher config value: {ctx.Configuration["CertifiedMembersFetcher"] ?? "null"}")
        };

        _ = ctx.Configuration["EmailMembershipNumberMapper"] switch
        {
            "Dummy" => services.AddTransient<IEmailMembershipNumberMapper, DummyEmailMembershipNumberMapper>(),
            "Moodle" => services.AddSingleton<IEmailMembershipNumberMapper, MoodleEmailMembershipNumberMapper>(),
            "Ms365" => throw new NotImplementedException("Ms365 mail to membership number mapping not yet implemented"),
            _ => throw new Exception($"Unknown EmailMembershipNumberMapper config value: {ctx.Configuration["EmailMembershipNumberMapper"] ?? "null"}")
        };

        _ = ctx.Configuration["RequiredMembersFetcher"] switch
        {
            "Dummy" => services.AddTransient<IRequiredMembersFetcher, DummyRequiredMembersFetcher>(),
            "Tipi" => services.AddTransient<IRequiredMembersFetcher, TipiRequiredMembersFetcher>(),
            _ => throw new Exception($"Unknown RequiredMembersFetcher config value: {ctx.Configuration["RequiredMembersFetcher"] ?? "null"}")
        };

        _ = ctx.Configuration["NotificationSender"] switch
        {
            "Dummy" => services
                        .AddTransient<INotificationSender, DummyNotificationSender>()
                        .AddTransient<ISummarySender, DummySummarySender>(),
            "Smtp" => services
                        .AddTransient<INotificationSender, SmptNotificationSender>()
                        .AddTransient<ISummarySender, SmtpSummarySender>(),
            _ => throw new Exception($"Unknown NotificationSender config value: {ctx.Configuration["NotificationSender"] ?? "null"}")
        };

        services.AddOptions<SafeFromHarmOptions>()
            .BindConfiguration("SafeFromHarm")
            .Validate(sfh => sfh.CertificateExpiryDays > 0);

        services.AddTransient<MissingCertificationsNotifier>();
    })
    .ConfigureMoodleServices()
    .ConfigureSmtp()
    .ConfigureTipi()
    .Build();

host.Run();
