using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
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
    IAccountCreationResultPublisher publisher)
{
    public async IAsyncEnumerable<AccountCreationResult> CreateAccounts(IEnumerable<Member> members, string requestorEmail, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AccountCreationResult result;
            try
            {
                result = await CreateAccount(member, requestorEmail, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to create account for member {membershipId}: {message}", member.MembershipNumber, ex.Message);
                result = new(member, null, AccountCreationResult.ResultType.OtherError);
            }

            yield return result;
        }
    }

    private async Task<AccountCreationResult> CreateAccount(Member member, string requestorEmail, CancellationToken cancellationToken)
    {
        var fetchedMember = await membersFetcher.GetMember(member.MembershipNumber, cancellationToken);
        if (fetchedMember != member)
            return new(member, null, AccountCreationResult.ResultType.MemberNotInTipi);

        cancellationToken.ThrowIfCancellationRequested();

        if (await mailChecker.HasEmailAccount(member.MembershipNumber, cancellationToken))
            return new(member, null, AccountCreationResult.ResultType.MemberHasMs365);

        var password = GeneratePassword();

        cancellationToken.ThrowIfCancellationRequested();

        var result = new AccountCreationResult(member, password, await creator.CreateAccount(member, password));

        if (result.Result == AccountCreationResult.ResultType.Success)
            await publisher.PublishResult(result, requestorEmail);

        return result;
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
