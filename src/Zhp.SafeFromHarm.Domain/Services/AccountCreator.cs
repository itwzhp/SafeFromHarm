using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Domain.Services;

public class AccountCreator(
    ILogger<AccountCreator> logger,
    IMembersFetcher membersFetcher,
    IMemberMailAccountChecker mailChecker,
    IAccountCreator creator,
    IEnumerable<IAccountCreationResultPublisher> publishers,
    PasswordGenerator generator)
{
    public async Task<IReadOnlyCollection<AccountCreationResult>> CreateAccounts(IEnumerable<Member> members, string requestorEmail, CancellationToken cancellationToken)
    {
        var result = new ConcurrentBag<AccountCreationResult>();

        await Parallel.ForEachAsync(
            members,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = 4
            }, 
            async (member, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    result.Add(await CreateAccount(member, cancellationToken));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unable to create account for member {membershipId}: {message}", member.MembershipNumber, ex.Message);
                    result.Add(new(member, null, AccountCreationResult.ResultType.OtherError));
                }
            });

        await Task.WhenAll(publishers.Select(async p => await p.PublishResult(result, requestorEmail)));

        return result;
    }

    private async Task<AccountCreationResult> CreateAccount(Member requestedMember, CancellationToken cancellationToken)
    {
        var fetchedMember = await membersFetcher.GetMember(requestedMember.MembershipNumber, cancellationToken);
        if (fetchedMember != requestedMember)
            return new(requestedMember, null, AccountCreationResult.ResultType.MemberNotInTipi);

        cancellationToken.ThrowIfCancellationRequested();

        if (await mailChecker.HasEmailAccount(requestedMember.MembershipNumber, cancellationToken))
            return new(requestedMember, null, AccountCreationResult.ResultType.MemberHasMs365);

        var password = generator.GeneratePassword();

        cancellationToken.ThrowIfCancellationRequested();

        return new AccountCreationResult(requestedMember, password, await creator.CreateAccount(fetchedMember, password));
    }
}
