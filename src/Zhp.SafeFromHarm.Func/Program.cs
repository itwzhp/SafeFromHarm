using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zhp.SafeFromHarm.Func.Infrastructure;


var host = new HostBuilder()
    .ConfigureSafeFromHarmHost()
    .Build();

if (host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
    host.AssertFunctionRegistrations();

host.Run();
