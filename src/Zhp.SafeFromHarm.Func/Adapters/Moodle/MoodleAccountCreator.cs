using Microsoft.Extensions.Options;
using Zhp.SafeFromHarm.Domain;
using Zhp.SafeFromHarm.Domain.Model;
using Zhp.SafeFromHarm.Domain.Ports.AccountCreation;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.Infrastructure;
using Zhp.SafeFromHarm.Func.Adapters.Moodle.ResponseContracts;

namespace Zhp.SafeFromHarm.Func.Adapters.Moodle;

internal class MoodleAccountCreator(MoodleClient client, IOptions<SafeFromHarmOptions> options) : IAccountCreator
{
    public async Task<AccountCreationResult.ResultType> CreateAccount(Member member, string password)
    {
        var username = member.MembershipNumber.ToLower();

        var existingUsers = await client.CallMoodle<GetUserResult>(MoodleFunctions.core_user_get_users, new()
        {
            ["criteria[0][key]"] = "username",
            ["criteria[0][value]"] = username,
        });

        if(existingUsers.Users.Length > 0)
            return AccountCreationResult.ResultType.MemberAlreadyHasMoodle;

        var creationResult = await client.CallMoodle<UserCreated[]>(MoodleFunctions.core_user_create_users, new()
        {
            ["users[0][username]"] = username,
            ["users[0][firstname]"] = member.FirstName,
            ["users[0][lastname]"] = member.LastName,
            ["users[0][email]"] = $"{member.MembershipNumber}@{options.Value.MoodleAccountMailFakeDomain}",
            ["users[0][password]"] = password,
            ["users[0][customfields][0][type]"] = "numer_ewidencyjny",
            ["users[0][customfields][0][value]"] = member.MembershipNumber,
        });

        return creationResult.SingleOrDefault()?.Username == username
            ? AccountCreationResult.ResultType.Success
            : AccountCreationResult.ResultType.OtherError;
    }
}
