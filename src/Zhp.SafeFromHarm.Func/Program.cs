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
            "Dummy" => services.AddSingleton<ICertifiedMembersFetcher, DummyCertifiedMembersFetcher>(),
            "Moodle" => services.AddSingleton<ICertifiedMembersFetcher, MoodleCertifiedMembersFetcher>(),
            _ => throw new Exception($"Unknown CertifiedMembersFetcher config value: {ctx.Configuration["CertifiedMembersFetcher"] ?? "null"}")
        };

        _ = ctx.Configuration["EmailMembershipNumberMapper"] switch
        {
            "Dummy" => services.AddSingleton<IEmailMembershipNumberMapper, DummyEmailMembershipNumberMapper>(),
            "Moodle" => services.AddSingleton<IEmailMembershipNumberMapper, MoodleEmailMembershipNumberMapper>(),
            "Ms365" => throw new NotImplementedException("Ms365 mail to membership number mapping not yet implemented"),
            _ => throw new Exception($"Unknown EmailMembershipNumberMapper config value: {ctx.Configuration["EmailMembershipNumberMapper"] ?? "null"}")
        };

        _ = ctx.Configuration["RequiredMembersFetcher"] switch
        {
            "Dummy" => services.AddSingleton<IRequiredMembersFetcher, DummyRequiredMembersFetcher>(),
            "Tipi" => services.AddSingleton<IRequiredMembersFetcher, TipiRequiredMembersFetcher>(),
            _ => throw new Exception($"Unknown RequiredMembersFetcher config value: {ctx.Configuration["RequiredMembersFetcher"] ?? "null"}")
        };

        _ = ctx.Configuration["NotificationSender"] switch
        {
            "Dummy" => services.AddSingleton<INotificationSender, DummyNotificationSender>(),
            "Smtp" => services.AddSingleton<INotificationSender, SmptNotificationSender>(),
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
