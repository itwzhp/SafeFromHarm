using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zhp.SafeFromHarm.Domain.Ports;
using Zhp.SafeFromHarm.Func.Adapters.Moodle;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;
using Zhp.SafeFromHarm.Func.Adapters.TestDummy;
using Zhp.SafeFromHarm.Func.Adapters.Tipi;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((ctx, config)
        => config
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", true))
    .ConfigureServices((ctx, services) =>
    {
        switch(ctx.Configuration["CertifiedMembersFetcher"])
        {
            case "Dummy":
                services.AddSingleton<ICertifiedMembersFetcher, DummyCertifiedMembersFetcher>();
                break;
            case "Moodle":
                services.AddSingleton<ICertifiedMembersFetcher, MoodleCertifiedMembersFetcher>();
                break;
            default:
                throw new Exception($"Unknown CertifiedMembersFetcher config value: {ctx.Configuration["CertifiedMembersFetcher"] ?? "null"}");
        }

        switch (ctx.Configuration["EmailMembershipNumberMapper"])
        {
            case "Dummy":
                services.AddSingleton<IEmailMembershipNumberMapper, DummyEmailMembershipNumberMapper>();
                break;
            case "Moodle":
                services.AddSingleton<IEmailMembershipNumberMapper, MoodleEmailMembershipNumberMapper>();
                break;
            case "Ms365":
                throw new NotImplementedException("Ms365 mail to membership number mapping not yet implemented");
            default:
                throw new Exception($"Unknown EmailMembershipNumberMapper config value: {ctx.Configuration["EmailMembershipNumberMapper"] ?? "null"}");
        }

        switch (ctx.Configuration["RequiredMembersFetcher"])
        {
            case "Dummy":
                services.AddSingleton<IRequiredMembersFetcher, DummyRequiredMembersFetcher>();
                break;
            case "Tipi":
                services.AddSingleton<IRequiredMembersFetcher, TipiRequiredMembersFetcher>();
                break;
            default:
                throw new Exception($"Unknown RequiredMembersFetcher config value: {ctx.Configuration["RequiredMembersFetcher"] ?? "null"}");
        }
    })
    .ConfigureMoodleServices()
    .Build();

//todo check if function class resolves

host.Run();
