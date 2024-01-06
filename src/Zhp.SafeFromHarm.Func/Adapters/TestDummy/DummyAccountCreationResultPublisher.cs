using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model.AccountCreation;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyAccountCreationResultPublisher(ILogger<DummyAccountCreationResultPublisher> log) : IAccountCreationResultPublisher
{
    public Task PublishResult(IReadOnlyCollection<AccountCreationResult> result, string requestorEmail)
    {
        log.LogInformation("Publishing result of account creation: {result}", result);
        return Task.CompletedTask;
    }
}
