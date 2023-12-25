using Zhp.SafeFromHarm.Domain.Model.AccountCreation;

namespace Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

public interface IAccountCreationResultPublisher
{
    Task PublishResult(IReadOnlyCollection<AccountCreationResult> result, string requestorEmail);
}
