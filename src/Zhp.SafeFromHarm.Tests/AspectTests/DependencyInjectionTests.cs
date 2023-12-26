using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
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
    [InlineData(typeof(GenerateReports))]
    public void CheckServiceBuild(Type functionType)
    {
        var host = new HostBuilder()
            .UseEnvironment(Environments.Development)
            .ConfigureSafeFromHarmHost()
            .ConfigureAppConfiguration(c =>
            {
                var userSecretsSource = c.Sources.Single(s => s is JsonConfigurationSource json && json.Path?.EndsWith("secrets.json") == true);
                c.Sources.Remove(userSecretsSource);

                c.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tipi:TokenId"] = "aaaa",
                    ["Tipi:TokenSecret"] = "aaaa",

                    ["Moodle:MoodleToken"] = "aaaa",

                    ["Smtp:Username"] = "test@example.com",
                    ["Smtp:Password"] = "asdf",
                });
            })
            .Build();

        host.Services.GetRequiredService(functionType)
            .Should().NotBeNull();
    }
}
