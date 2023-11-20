namespace Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

public interface IMemberMailAccountChecker
{
    Task<bool> HasEmailAccount(string membershipId, CancellationToken cancellationToken);
}
