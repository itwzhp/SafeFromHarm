using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zhp.SafeFromHarm.Func.Functions;
using Zhp.SafeFromHarm.Func.Infrastructure;

namespace Zhp.SafeFromHarm.Tests.AspectTests;

public class DependencyInjectionTests
{
    [Theory]
    [InlineData(typeof(CreateAccounts))]
    [InlineData(typeof(FindMissingRequiredCertifications))]
    public void CheckServiceBuild(Type functionType)
    {
        var host = new HostBuilder()
            .UseEnvironment(Environments.Development)
            .ConfigureSafeFromHarmHost()
            .Build();

        host.Services.GetRequiredService(functionType)
            .Should().NotBeNull();
    }
}
