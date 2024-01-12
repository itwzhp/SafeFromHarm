using Zhp.SafeFromHarm.Domain.Model.AccountCreation;

namespace Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

public interface IAccountCreator
{
    Task<AccountCreationResult.ResultType> CreateAccount(Member member, string password);
}
