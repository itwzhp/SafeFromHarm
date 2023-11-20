using Zhp.SafeFromHarm.Domain.Model;

namespace Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

public interface IAccountCreationResultPublisher
{
    Task PublishResult(AccountCreationResult result, string requestorEmail);
}
