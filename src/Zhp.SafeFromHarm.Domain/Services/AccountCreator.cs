using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;

namespace Zhp.SafeFromHarm.Domain.Services;

public class AccountCreator(
    RandomNumberGenerator generator,
    ILogger<AccountCreator> logger,
    IMembersFetcher membersFetcher,
    IMemberMailAccountChecker mailChecker,
    IAccountCreator creator,
    IEnumerable<IAccountCreationResultPublisher> publishers)
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

    private async Task<AccountCreationResult> CreateAccount(Member member, CancellationToken cancellationToken)
    {
        var fetchedMember = await membersFetcher.GetMember(member.MembershipNumber, cancellationToken);
        if (fetchedMember != member)
            return new(member, null, AccountCreationResult.ResultType.MemberNotInTipi);

        cancellationToken.ThrowIfCancellationRequested();

        if (await mailChecker.HasEmailAccount(member.MembershipNumber, cancellationToken))
            return new(member, null, AccountCreationResult.ResultType.MemberHasMs365);

        var password = GeneratePassword();

        cancellationToken.ThrowIfCancellationRequested();

        return new AccountCreationResult(member, password, await creator.CreateAccount(member, password));
    }

    private string GeneratePassword()
    {
        const int length = 8;
        // full alphanumeric ext. 0, l, I
        const string easyChars = "123456789abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";

        byte[] byteBuffer = new byte[length];
        generator.GetBytes(byteBuffer);
        var password = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            int randomValue = byteBuffer[i] % easyChars.Length;
            password.Append(easyChars[randomValue]);
        }

        return password.ToString();
    }
}
