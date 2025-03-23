using Microsoft.Extensions.Hosting;
using Zhp.SafeFromHarm.Func.Infrastructure;


var host = new HostBuilder()
    .ConfigureSafeFromHarmHost()
    .Build();

host.Run();
