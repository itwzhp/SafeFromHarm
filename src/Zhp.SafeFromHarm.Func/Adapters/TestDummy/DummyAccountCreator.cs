using Microsoft.Extensions.Logging;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Func.Adapters.TestDummy;

internal class DummyAccountCreator(ILogger<DummyAccountCreator> logger) : IAccountCreator
{
    public Task<AccountCreationResult.ResultType> CreateAccount(Member member, string password)
    {
        var result = member.MembershipNumber.Length % 2 == 0
            ? AccountCreationResult.ResultType.Success
            : AccountCreationResult.ResultType.MemberAlreadyHasMoodle;

        logger.LogInformation("Dummy: Creating account for {member}: {result}", member, result);

        return Task.FromResult(result);
    }
}
